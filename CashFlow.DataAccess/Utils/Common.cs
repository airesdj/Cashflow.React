using CashFlow.Core.Classes;

namespace CashFlow.DataAccess.Utils
{
    public class Common
    {
        private static AESEncryptor logAES = null!;

        private static void AesInitialize()
        {
            if (logAES == null)
            {
                logAES = new AESEncryptor();
            }
        }

        public static string AesEncrypt(string text)
        {
            AesInitialize();
            return logAES.Encrypt(text);
        }

        public static string AesDecrypt(string text)
        {
            AesInitialize();
            return logAES.Decrypt(text);
        }
    }
}