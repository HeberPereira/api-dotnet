﻿using hb29.API.Controllers;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace hb29.API.Helpers
{
    
    public class Cryptography
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Cryptography> _logger;
        public byte[] key;
        public byte[] iv;

        public Cryptography(IConfiguration configuration, ILogger<Cryptography> logger)
        {
            _configuration = configuration;
            _logger = logger;
            string skey = _configuration["Cryptography-Key"];
            string siv = _configuration["Cryptography-IV"];
            key = Convert.FromBase64String(_configuration["Cryptography-Key"]);
            iv = Convert.FromBase64String(_configuration["Cryptography-IV"]);
        }



        public string Encrypt(string word)
        {
            using (Aes myAes = Aes.Create())
            {

                myAes.Key = key;
                myAes.IV = iv;

                if (word != null)
                {
                    // Encrypt the string to an array of bytes.
                    byte[] encrypted = EncryptStringToBytes_Aes(word, myAes.Key, myAes.IV);

                    return Convert.ToBase64String(encrypted);
                }

                return null;
                
            }
        }

        public string Decrypt(string word)
        {
            using (Aes myAes = Aes.Create())
            {
                myAes.Key = key;
                myAes.IV = iv;

                if (word != null)
                {
                    byte[] encrypted = TryParseFromBase64String(word);

                    // Decrypt the bytes to a string.
                    if (encrypted != null)
                    {
                        return DecryptStringFromBytes_Aes(encrypted, myAes.Key, myAes.IV);
                    }
                    else
                    {
                        return String.Empty;
                    }
                }

                return null;
             
            }
        }

        public byte[] TryParseFromBase64String(string PassWord)
        {
            if (PassWord == null) throw new ArgumentNullException("PassWord");

            if ((PassWord.Length % 4 == 0) && _rx.IsMatch(PassWord))
            {
                try
                {
                    return Convert.FromBase64String(PassWord);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error on TryParseFromBase64String" + ex.Message);
                }
            }
            return null;
        }

        private static readonly Regex _rx = new Regex(
            @"^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}[AEIMQUYcgkosw048]=|[A-Za-z0-9+/][AQgw]==)?$",
            RegexOptions.Compiled);

        public byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");
            byte[] encrypted;

            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                // Create an encryptor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }

            // Return the encrypted bytes from the memory stream.
            return encrypted;
        }

        public string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");

            // Declare the string used to hold
            // the decrypted text.
            string plaintext = null;

            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;
                

                // Create a decryptor to perform the stream transform.
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for decryption.
                try
                {
                    using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                    {
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                            {

                                // Read the decrypted bytes from the decrypting stream
                                // and place them in a string.
                                plaintext = srDecrypt.ReadToEnd();
                            }
                        }
                    }
                }
                catch(Exception ex)
                {
                    _logger.LogError("Error on Decryptography : " + ex.Message);
                    return null;
                }
            }

            return plaintext;
        }
    }
}
