namespace DrimAgents.Api.Common.Services;

public interface IPaginationEncryption
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}
