namespace DrimAgents.Api.Common.Services;

public interface IDataProtectionEncryption
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}
