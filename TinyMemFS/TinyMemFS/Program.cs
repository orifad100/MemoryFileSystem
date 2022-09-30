using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace TinyMemFS
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }

    class TinyMemFS
    {

        private Dictionary<string, List<String>> dictionaryFile;
        private Dictionary<string, List<String>> EcryptDic;
        private const string mysecurityKey = "MyNewKey";
        private ReaderWriterLockSlim lockF = new ReaderWriterLockSlim();




        public TinyMemFS()
        {
            dictionaryFile = new Dictionary<string, List<String>>();
            EcryptDic = new Dictionary<string, List<String>>();
         
        }

        // fileName - The name of the file to be added to the file system
        // fileToAdd - The file path on the computer that we add to the system
        // return false if operation failed for any reason
        // Example:
        // add("name1.pdf", "C:\\Users\\user\Desktop\\report.pdf")
        // note that fileToAdd isn't the same as the fileName
        public bool add(String fileName, String fileToAdd)
        {
            lockF.EnterWriteLock();
            try
            {
                if (fileName == fileToAdd)
                    return false;

                String size, dayAndCreationDay, CreationTime;

                try
                {
                    String path = fileToAdd;//path of the file
                    FileAttributes attributes = File.GetAttributes(path);//check if the file exists
                    FileInfo fi = new FileInfo(path);// get information of the file
                    size = (fi.Length / 1024).ToString() + "KB";//get the size of the file
                    DateTime creation = File.GetCreationTime(path);//create date of the file
                    dayAndCreationDay = creation.ToLongDateString();// Tuesday, 21 June 2022
                    CreationTime = creation.ToShortTimeString();//18:52:34
                    List<String> list = new List<String>();//list that contain the meta data of the file
                    list.Add(path);//add Path
                    list.Add(size);//add size
                    list.Add(dayAndCreationDay);//add date
                    list.Add(CreationTime);
                    dictionaryFile.Add(fileName, list);//adding file name and file path to the Dictionary, Key=Name and Value=list of Meta data. 
                }
                catch (Exception e)
                {
                    Console.WriteLine("This file does not exist.");
                    return false;

                }

                return true;
            }
            finally
            {
                lockF.ExitWriteLock();
            }
           
        }

        // fileName - remove fileName from the system
        // this operation releases all allocated memory for this file
        // return false if operation failed for any reason
        // Example:
        // remove("name1.pdf")
        public bool remove(String fileName)
        {
            lockF.EnterWriteLock();
            try
            {
                bool keyExists = dictionaryFile.ContainsKey(fileName);
                if (keyExists == false)
                    return false;
                dictionaryFile.Remove(fileName);//Remove from the fictionary 
                return true;
            }
            finally
            {
                lockF.ExitWriteLock();
            }
          
        }

        // The function returns a list of strings with the file information in the system
        // Each string holds details of one file as following: "fileName,size,creation time"
        // Example:{
        // "report.pdf,630KB,Friday, ‎May ‎13, ‎2022, ‏‎12:16:32 PM",
        // "table1.csv,220KB,Monday, ‎February ‎14, ‎2022, ‏‎8:38:24 PM" }
        // You can use any format for the creation time and date
        public List<String> listFiles()
        {
            lockF.EnterReadLock();
            try
            {
                List<String> files = new List<string>();
                foreach (var item in dictionaryFile)
                {
                    List<String> list = item.Value;
                    list[0] = item.Key;
                    string Metadata = string.Join(",", list);
                    files.Add(Metadata);
                }

                return files;
            }
            finally
            {
                lockF.ExitReadLock();
            }
     
        }

        // this function saves file from the TinyMemFS file system into a file in the physical disk
        // fileName - file name from TinyMemFS to save in the computer
        // fileToAdd - The file path to be saved on the computer
        // return false if operation failed for any reason
        // Example:
        // save("name1.pdf", "C:\\tmp\\fileName.pdf")
        public bool save(String fileName, String fileToAdd)
        {
            lockF.EnterReadLock();
            try
            {
                bool keyExists = dictionaryFile.ContainsKey(fileName);//check if the file exists in our system .
                if (keyExists == false)
                    return false;
                String path = dictionaryFile[fileName][0];//Get the path of the file
                string pathString = System.IO.Path.Combine(fileToAdd, fileName);
                foreach (KeyValuePair<String, List<String>> pair in EcryptDic)
                {
                    if (pair.Value.Contains(fileName) == true)
                    {
                        Console.WriteLine("This file has  encrypted ");
                        return false;
                    }
                }
                try
                {
                    FileStream fs = File.Create(pathString);
                    fs.Close();

                }
                catch (Exception e)
                {
                    Console.WriteLine("File \"{0}\" did not create", fileName);

                }
                File.Copy(path, pathString, true);//Copy content .
                return true;
            }
            finally
            {
                lockF.ExitReadLock();
            }
           

        }
        // key - Encryption key to encrypt the contents of all files in the system 
        // You can use an encryption algorithm of your choice
        // return false if operation failed for any reason
        // Example:
        // encrypt("myFSpassword")
        public Boolean encrypt(String key)
        {
            if (key == "")
                return false;
            lockF.EnterWriteLock();
            try
            {
                byte[] MyEncryptedArray = UTF8Encoding.UTF8.GetBytes(key);

                MD5CryptoServiceProvider MyMD5CryptoService = new
                   MD5CryptoServiceProvider();

                byte[] MysecurityKeyArray = MyMD5CryptoService.ComputeHash
                   (UTF8Encoding.UTF8.GetBytes(mysecurityKey));

                MyMD5CryptoService.Clear();

                var MyTripleDESCryptoService = new
                   TripleDESCryptoServiceProvider();

                MyTripleDESCryptoService.Key = MysecurityKeyArray;

                MyTripleDESCryptoService.Mode = CipherMode.ECB;

                MyTripleDESCryptoService.Padding = PaddingMode.PKCS7;

                var MyCrytpoTransform = MyTripleDESCryptoService
                   .CreateEncryptor();

                byte[] MyresultArray = MyCrytpoTransform
                   .TransformFinalBlock(MyEncryptedArray, 0,
                   MyEncryptedArray.Length);

                MyTripleDESCryptoService.Clear();

                string ecryptket = Convert.ToBase64String(MyresultArray, 0, MyresultArray.Length);
                List<String> listEcrypt = new List<string>();

                if (ecryptket == null)
                    return false;
                foreach (string file in dictionaryFile.Keys)
                    listEcrypt.Add(file);
                EcryptDic.Add(ecryptket, listEcrypt);
                return true;
            }
            finally
            {
                lockF.ExitWriteLock();
            }
              
            
        }

        public bool decrypt(String key)
        {
            if (key == "")
                return false;
            lockF.EnterWriteLock();
            try
            {
                List<String> list = new List<String>();
                // fileName - Decryption key to decrypt the contents of all files in the system 
                // return false if operation failed for any reason
                // Example:
                // decrypt("myFSpassword")
                if (EcryptDic.Count == 0)
                    return false;
                else
                {
                    int count = EcryptDic.Count - 1;
                    int i = 0;
                    foreach (KeyValuePair<String, List<String>> pair in EcryptDic)
                    {
                        
                        if(i==count)
                            list.Add(pair.Key);
                        
                        i++;


                    }


                }

               
                byte[] MyDecryptArray = Convert.FromBase64String(list[0]);

                MD5CryptoServiceProvider MyMD5CryptoService = new
                    MD5CryptoServiceProvider();

                byte[] MysecurityKeyArray = MyMD5CryptoService.ComputeHash
                    (UTF8Encoding.UTF8.GetBytes(mysecurityKey));

                MyMD5CryptoService.Clear();

                var MyTripleDESCryptoService = new
                    TripleDESCryptoServiceProvider();

                MyTripleDESCryptoService.Key = MysecurityKeyArray;

                MyTripleDESCryptoService.Mode = CipherMode.ECB;

                MyTripleDESCryptoService.Padding = PaddingMode.PKCS7;

                var MyCrytpoTransform = MyTripleDESCryptoService
                    .CreateDecryptor();

                byte[] MyresultArray = MyCrytpoTransform
                    .TransformFinalBlock(MyDecryptArray, 0,
                    MyDecryptArray.Length);

                MyTripleDESCryptoService.Clear();

                string Decrypt = UTF8Encoding.UTF8.GetString(MyresultArray);
                if (Decrypt == null)
                    return false;
                if (Decrypt == key)
                {
                    EcryptDic.Remove(list[0]);
                    return true;
                }
                
                return false;//need to be reutrn false

            }
            finally
            {
                lockF.ExitWriteLock();
            }
         
        }

        // ************** NOT MANDATORY ********************************************
        // ********** Extended features of TinyMemFS ********************************
        public bool saveToDisk(String fileName)
        {
            /*
             * Save the FS to a single file in disk
             * return false if operation failed for any reason
             * You should store the entire FS (metadata and files) from memory to a single file.
             * You can decide how to save the FS in a single file (format, etc.) 
             * Example:
             * SaveToDisk("MYTINYFS.DAT")
             */
            return false;
        }


        public bool loadFromDisk(String fileName)
        {
            /*
             * Load a saved FS from a file  
             * return false if operation failed for any reason
             * You should clear all the files in the current TinyMemFS if exist, before loading the filenName
             * Example:
             * LoadFromDisk("MYTINYFS.DAT")
             */
            return false;
        }

        public bool compressFile(String fileName)
        {
            /* Compress file fileName
             * return false if operation failed for any reason
             * You can use an compression/uncompression algorithm of your choice
             * Note that the file size might be changed due to this operation, update it accordingly
             * Example:
             * compressFile ("name1.pdf");
             */
            return false;
        }

        public bool uncompressFile(String fileName)
        {
            /* uncompress file fileName
             * return false if operation failed for any reason
             * You can use an compression/uncompression algorithm of your choice
             * Note that the file size might be changed due to this operation, update it accordingly
             * Example:
             * uncompressFile ("name1.pdf");
             */
            return false;
        }

        public bool setHidden(String fileName, bool hidden)
        {
            /* set the hidden property of fileName
             * If file is hidden, it will not appear in the listFiles() results
             * return false if operation failed for any reason
             * Example:
             * setHidden ("name1.pdf", true);
             */
            return false;
        }

        public bool rename(String fileName, bool newFileName)
        {
            /* Rename filename to newFileName
             * Return false if operation failed for any reason (E.g., newFileName already exists)
             * Example:
             * rename ("name1.pdf", "name2.pdf");
             */
            return false;
        }

        public bool copy(String fileName1, bool fileName2)
        {
            /* Rename filename1 to a new filename2
             * Return false if operation failed for any reason (E.g., fileName1 doesn't exist or filename2 already exists)
             * Example:
             * rename ("name1.pdf", "name2.pdf");
             */
            return true;
        }


        public void sortByName()
        {
            /* Sort the files in the FS by their names (alphabetical order)
             * This should affect the order the files appear in the listFiles 
             * if two names are equal you can sort them arbitrarily
             */
            return;
        }

        public void sortByDate()
        {
            /* Sort the files in the FS by their date (new to old)
             * This should affect the order the files appear in the listFiles  
             * if two dates are equal you can sort them arbitrarily
             */
            return;
        }

        public void sortBySize()
        {
            /* Sort the files in the FS by their sizes (large to small)
             * This should affect the order the files appear in the listFiles  
             * if two sizes are equal you can sort them arbitrarily
             */
            return;
        }


        public bool compare(String fileName1, String fileName2)
        {
            /* compare fileName1 and fileName2
             * files considered equal if their content is equal 
             * Return false if the two files are not equal, or if operation failed for any reason (E.g., fileName1 or fileName2 not exist)
             * Example:
             * compare ("name1.pdf", "name2.pdf");
             */
            return false;
        }

        public Int64 getSize()
        {
            /* return the size of all files in the FS (sum of all sizes)
             */
            return 0;
        }


    }

}
