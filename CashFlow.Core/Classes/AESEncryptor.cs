using System.Security.Cryptography;
using System.Text;

namespace CashFlow.Core.Classes
{
    public class AESEncryptor : IDisposable
    {
        private static string EncryptKeyLog = "CASHFLOW.ENCRYPT.KEY.AES.COMPONENT.ENCRYPTOR";
        private byte[] keyBytesDefault = new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 };
        private byte[] mKey = Array.Empty<byte>();
        private byte[] mIV = Array.Empty<byte>();
        private int keySizeInBits = 0;
        private bool disposedValue;

        public AESEncryptor()
        {
            keySizeInBits = 256;  // Default to AES-256
            SetKey(EncryptKeyLog);
        }

        public AESEncryptor(byte[] key, byte[] IV) : this()
        {
            mKey = key;
            mIV = IV;
        }

        public void SetKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            using (var pdb = new Rfc2898DeriveBytes(key, keyBytesDefault, 1000, HashAlgorithmName.SHA1))
            {
                mKey = pdb.GetBytes(32); // 256 bits
                mIV = pdb.GetBytes(16);  // 128 bits
            }
        }

        public string Encrypt(string data)
        {
            if (string.IsNullOrWhiteSpace(data)) return string.Empty;
            else
            {
                byte[] dataBytes = Array.Empty<byte>();
                byte[] encryptedBytes = Array.Empty<byte>();

                try
                {
                    using (var aesAlg = Aes.Create())
                    {
                        aesAlg.KeySize = keySizeInBits;
                        aesAlg.Padding = PaddingMode.PKCS7;

                        using (var encryptor = aesAlg.CreateEncryptor(mKey, mIV))
                        {
                            dataBytes = Encoding.Unicode.GetBytes(data);
                            encryptedBytes = encryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length);
                        }
                    }

                    return Convert.ToBase64String(encryptedBytes);
                }
                catch
                {
                    return string.Empty;
                }
                finally
                {
                    dataBytes = Array.Empty<byte>();
                    encryptedBytes = Array.Empty<byte>();
                }
            }
        }

        public string Decrypt(string data)
        {
            if (string.IsNullOrWhiteSpace(data)) return string.Empty;
            else
            {
                byte[] dataBytes = Array.Empty<byte>();
                byte[] plaintextBytes = Array.Empty<byte>();

                try
                {
                    using (var aesAlg = Aes.Create())
                    {
                        aesAlg.KeySize = keySizeInBits;
                        aesAlg.Padding = PaddingMode.PKCS7;

                        using (var decryptor = aesAlg.CreateDecryptor(mKey, mIV))
                        {
                            if (data.Length % 4 != 0)
                                data = data + new string('=', 4 - data.Length % 4);  // Fix padding

                            dataBytes = Convert.FromBase64String(data);
                            plaintextBytes = decryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length);
                        }
                    }

                    return Encoding.Unicode.GetString(plaintextBytes);
                }
                catch
                {
                    return string.Empty;
                }
                finally
                {
                    dataBytes = Array.Empty<byte>();
                    plaintextBytes = Array.Empty<byte>();
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Array.Clear(keyBytesDefault, 0, keyBytesDefault.Length);
                    Array.Clear(mKey, 0, mKey.Length);
                    Array.Clear(mIV, 0, mIV.Length);
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        ~AESEncryptor() => Dispose(disposing: false);
    }
}