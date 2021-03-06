﻿using System;

using System.IO;

using System.Text;

using System.Security.Cryptography;

using System.Reflection;

using System.Windows.Forms;

using System.Runtime.Serialization.Formatters.Soap;

namespace Structures

{
    #region SMTPSettings class

    [Serializable]

    public sealed class SmtpSettings

    {
        private volatile static SmtpSettings instance;

        private static object syncObj = new object();

        private string smtpPassword = "";

        private string prefsDir = "";

        public string SmtpServer { get; set; } = "";

        public string SmtpDomain { get; set; } = "";

        public string SmtpUsername { get; set; } = "";

        public string SmtpPassword {
            get
            {
                if (smtpPassword?.Length == 0)
                {
                    return "";
                }
                else
                {
                    Assembly asm = Assembly.GetExecutingAssembly();
                    return DecryptString(smtpPassword, asm.GetType().GUID.ToString());
                }
            }
            set {
                if (value?.Length == 0)
                {
                    smtpPassword = "";
                }else{
                    Assembly asm = Assembly.GetExecutingAssembly();

                smtpPassword =EncryptString(value, asm.GetType().GUID.ToString());
                }
            }
        }

        public int SmtpPort { get; set; } = 25;

        public string FromEmail { get; set; } = "";

        public string ToEmail { get; set; } = "";

        public string CCEmail { get; set; } = "";

        public bool UseNetworkCredentials { get; set; } = false;

        public bool UseSSL { get; set; } = false;

        public bool SavePassword { get; set; } = false;

        private SmtpSettings() {}

        public static SmtpSettings Instance
        {
            get
            {
                // only create a new instance if one doesn't already exist.

                if (instance == null)

                    // use this lock to ensure that only one thread is access this block of code at once.

                    lock (syncObj)
                        if (instance == null)
                            instance = new SmtpSettings();

                // return instance where it was just created or already existed.

                return instance;
            }

            set { instance = value; }
        }

        public void Save(string filename)
        {
            if (prefsDir?.Length == 0)
            {
                prefsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MySEQ");
            }

            // Dont save password if we are not selecting to save it
            string curpass = "";
            if (!Instance.SavePassword) {
                curpass = Instance.SmtpPassword;
                Instance.SmtpPassword = "";
            }

            FileStream fs = new FileStream(filename, FileMode.Create);

            SoapFormatter sf1 = new SoapFormatter();

            sf1.Serialize(fs, Instance);

            if (!Instance.SavePassword) {
                Instance.SmtpPassword = curpass;
                curpass = "";
            }

            fs.Close();

            string oldconfigFile = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "myseq.xml");
            if (File.Exists(oldconfigFile))
                File.Delete(oldconfigFile);
        }

        public void Load(string filename) {
            FileStream fs = null;

            try {
                fs = new FileStream(filename, FileMode.Open);

                SoapFormatter sf1 = new SoapFormatter();

                Instance = (SmtpSettings)sf1.Deserialize(fs);

                Instance.prefsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MySEQ");
            }
            catch (Exception ex) { LogLib.WriteLine("Error in SmtpSettings.Load(): ", ex); }

            fs?.Close();
        }

        public static string DecryptString(string Message, string _Pass)
        {
            byte[] _Res;

            UTF8Encoding _UTF8 = new UTF8Encoding();
            MD5CryptoServiceProvider _Hash = new MD5CryptoServiceProvider();

            byte[] _Key = _Hash.ComputeHash(_UTF8.GetBytes(_Pass));
            byte[] _Data = Convert.FromBase64String(Message);

            TripleDESCryptoServiceProvider _Service = new TripleDESCryptoServiceProvider
            {
                Key = _Key,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            };

            try
            {
                ICryptoTransform _Decrypter = _Service.CreateDecryptor();
                _Res = _Decrypter.TransformFinalBlock(_Data, 0, _Data.Length);
            }
            catch (Exception ex)
            {
                LogLib.WriteLine("Error in DecryptString(): ", ex);

                return "";
            }
            finally
            {
                _Hash.Clear();
                _Service.Clear();
            }

            return _UTF8.GetString(_Res);
        }

        public static string EncryptString(string Message, string _Pass)
        {
            byte[] _Res;

            UTF8Encoding _UTF8 = new UTF8Encoding();
            MD5CryptoServiceProvider _Hash = new MD5CryptoServiceProvider();

            byte[] _Key = _Hash.ComputeHash(_UTF8.GetBytes(_Pass));
            byte[] _Data = _UTF8.GetBytes(Message);

            TripleDESCryptoServiceProvider _Service = new TripleDESCryptoServiceProvider
            {
                Key = _Key,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            };

            try
            {
                ICryptoTransform _Encrypter = _Service.CreateEncryptor();
                _Res = _Encrypter.TransformFinalBlock(_Data, 0, _Data.Length);
            }
            catch (Exception ex)
            {
                LogLib.WriteLine("Error in EncryptString(): ", ex);
                return "";
            }
            finally
            {
                _Hash.Clear();
                _Service.Clear();
            }
            return Convert.ToBase64String(_Res);
        }
    }

#endregion
#region Settings class

    #endregion

}