//using Microsoft.Win32;
//using System;
//using static PicView.lib.NativeMethods;

//namespace PicView.lib
//{
//    internal static class FileAssociation
//    {
//        public static void SetAssociation(string Extension, string KeyName, string OpenWith, string FileDescription)
//        {
//            RegistryKey BaseKey;
//            RegistryKey OpenMethod;
//            RegistryKey Shell;
//            RegistryKey CurrentUser;

//            BaseKey = Registry.ClassesRoot.CreateSubKey(Extension);
//            BaseKey.SetValue("", KeyName);

//            OpenMethod = Registry.ClassesRoot.CreateSubKey(KeyName);
//            OpenMethod.SetValue("", FileDescription);
//            OpenMethod.CreateSubKey("DefaultIcon").SetValue("", "\"" + OpenWith + "\",0");
//            Shell = OpenMethod.CreateSubKey("Shell");
//            Shell.CreateSubKey("edit").CreateSubKey("command").SetValue("", "\"" + OpenWith + "\"" + " \"%1\"");
//            Shell.CreateSubKey("open").CreateSubKey("command").SetValue("", "\"" + OpenWith + "\"" + " \"%1\"");
//            BaseKey.Close();
//            OpenMethod.Close();
//            Shell.Close();

//            CurrentUser = Registry.CurrentUser.CreateSubKey(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.ucs");
//            CurrentUser = CurrentUser.OpenSubKey("UserChoice", RegistryKeyPermissionCheck.ReadWriteSubTree, System.Security.AccessControl.RegistryRights.FullControl);
//            CurrentUser.SetValue("Progid", KeyName, RegistryValueKind.String);
//            // delete the key instead of trying to change it
//            CurrentUser = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\FileExts\\.ucs", true);
//            CurrentUser.DeleteSubKey("UserChoice", false);
//            CurrentUser.Close();

//            // Tell explorer the file association has been changed
//            SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
//        }
//    }


//}
