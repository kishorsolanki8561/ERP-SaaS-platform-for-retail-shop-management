import { Injectable, OnDestroy, inject, signal, computed } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../../environments/environment';

export interface DeviceStatusEvent {
  deviceId: string;
  isOnline: boolean;
  lastSeenAtUtc: string;
}

export interface ReplicationJobEvent {
  deploymentId: string;
  status: string;
  rowsTransferred: number;
  rowsConflicted: number;
  updatedAtUtc: string;
}

@Injectable({ providedIn: 'root' })
export class SignalRService implements OnDestroy {
  private connection: signalR.HubConnection | null = null;

  private readonly _connected = signal(false);
  private readonly _deviceEvents = signal<DeviceStatusEvent[]>([]);
  private readonly _replicationEvents = signal<ReplicationJobEvent[]>([]);

  readonly isConnected = this._connected.asReadonly();
  readonly latestDeviceEvent = computed(() => this._deviceEvents().at(-1) ?? null);
  readonly latestReplicationEvent = computed(() => this._replicationEvents().at(-1) ?? null);

  async connect(shopId: number, token: string): Promise<void> {
    if (this.connection) return;

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiBaseUrl}/hubs/sync-status`, {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.connection.on('DeviceStatusChanged', (event: DeviceStatusEvent) => {
      this._deviceEvents.update(list => [...list.slice(-99), event]);
    });

    this.connection.on('ReplicationJobUpdated', (event: ReplicationJobEvent) => {
      this._replicationEvents.update(list => [...list.slice(-99), event]);
    });

    this.connection.onclose(() => this._connected.set(false));
    this.connection.onreconnected(() => this._connected.set(true));

    await this.connection.start();
    await this.connection.invoke('JoinShopGroup', String(shopId));
    this._connected.set(true);
  }

  async disconnect(): Promise<void> {
    if (!this.connection) return;
    await this.connection.stop();
    this.connection = null;
    this._connected.set(false);
  }

  ngOnDestroy(): void {
    void this.disconnect();
  }
}
