using System;
using System.IO;
using System.Security.Cryptography;

namespace SmartFileMan.Signer
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                ShowHelp();
                return;
            }

            string command = args[0].ToLower();

            try
            {
                switch (command)
                {
                    case "keygen":
                        GenerateKeys(args.Length > 1 ? args[1] : ".");
                        break;
                    case "sign":
                        if (args.Length < 3)
                        {
                            Console.WriteLine("Error: Missing arguments for sign command.");
                            ShowHelp();
                            return;
                        }
                        SignFile(args[1], args[2]);
                        break;
                    default:
                        ShowHelp();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }

        static void ShowHelp()
        {
            Console.WriteLine("SmartFileMan Plugin Signer Tool");
            Console.WriteLine("Usage:");
            Console.WriteLine("  keygen [output_dir]       Generate a new RSA key pair (private.key and public.key)");
            Console.WriteLine("  sign <dll_path> <key_path>  Sign a DLL file using the private key");
        }

        static void GenerateKeys(string outputDir)
        {
            if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

            using (var rsa = RSA.Create(2048))
            {
                string privateKey = rsa.ToXmlString(true);
                string publicKey = rsa.ToXmlString(false);

                string privPath = Path.Combine(outputDir, "private.key");
                string pubPath = Path.Combine(outputDir, "public.key");

                File.WriteAllText(privPath, privateKey);
                File.WriteAllText(pubPath, publicKey);

                Console.WriteLine($"Keys generated successfully in {outputDir}");
                Console.WriteLine($"Private Key: {privPath} (KEEP SECRET!)");
                Console.WriteLine($"Public Key:  {pubPath} (Distribute with App)");
            }
        }

        static void SignFile(string dllPath, string privateKeyPath)
        {
            if (!File.Exists(dllPath)) throw new FileNotFoundException("DLL file not found", dllPath);
            if (!File.Exists(privateKeyPath)) throw new FileNotFoundException("Private key file not found", privateKeyPath);

            string privateKey = File.ReadAllText(privateKeyPath);
            
            using (var rsa = RSA.Create())
            {
                rsa.FromXmlString(privateKey);
                
                byte[] data = File.ReadAllBytes(dllPath);
                byte[] signature = rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                
                string sigPath = dllPath + ".sig";
                File.WriteAllBytes(sigPath, signature);
                
                Console.WriteLine($"Successfully signed: {dllPath}");
                Console.WriteLine($"Signature created:   {sigPath}");
            }
        }
    }
}
