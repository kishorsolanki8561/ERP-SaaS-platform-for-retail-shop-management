namespace ErpSaas.Modules.ApiAccess.Services;

public interface IWebhookSignatureGenerator
{
    string Generate(string payloadJson, string signingSecret);
}
