CREATE OR ALTER PROCEDURE [sequence].[usp_AllocateSequenceNumber]
    @ShopId      BIGINT,
    @Code        NVARCHAR(50),
    @AllocatedNumber BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    -- Lock the row exclusively so concurrent calls queue up rather than racing.
    UPDATE [sequence].[SequenceDefinition]
    SET    [LastNumber] = [LastNumber] + 1
    WHERE  [ShopId] = @ShopId
      AND  [Code]   = @Code;

    IF @@ROWCOUNT = 0
    BEGIN
        -- Auto-create sequence on first use starting at 1.
        INSERT INTO [sequence].[SequenceDefinition]
               ([ShopId], [Code], [LastNumber], [PadLength],
                [CreatedAtUtc], [IsDeleted], [RowVersion])
        VALUES (@ShopId, @Code, 1, 6,
                GETUTCDATE(), 0, 0x);

        SET @AllocatedNumber = 1;
    END
    ELSE
    BEGIN
        SELECT @AllocatedNumber = [LastNumber]
        FROM   [sequence].[SequenceDefinition]
        WHERE  [ShopId] = @ShopId
          AND  [Code]   = @Code;
    END

    -- Audit trail in LogDb is written by the application layer (SequenceAllocation entity).

    COMMIT TRANSACTION;
END;
GO
