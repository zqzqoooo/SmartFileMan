using System;
using System.IO;
using System.Security.Cryptography;

namespace SmartFileMan.Core.Services
{
    public class PluginVerifier
    {
        // TODO: 替换为你生成的真实 RSA 公钥 (XML 格式)
        // 只有持有对应私钥的人签名的插件才能通过验证
        private const string PublicKeyXml = @"<RSAKeyValue><Modulus>pbFCtCZO6gfwqzkRnI6DgzAUMrV91O++3Zu8mmeEmI3aBcSGo5jOeJNwuuLPynVia6E7afYYgnxiYT88WzgGk970y2LTVg1g4zlsHLS6ACSuFry0vJ7vpFFIuoGF+CozCwlrGZyMSCa+MUemgbtw2znwAmVGloCjLACkzEkPB5Hv+qTq4y5ue6wQXaEXo5OzMukN8lYGF3wDwX9jP2MuPJplpuSdK35wgpGonOiT6dG87aQD5HGMca4Ck1z3wb/i8rdhS2hukV5ETQKywpNEztcCHIpI4BALScA34r08YVYVN9UHSSW+jE97w0fqHgY7Bz6NIw72hI6yQBChYC11mQ==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

        /// <summary>
        /// 验证插件 DLL 是否被篡改
        /// </summary>
        /// <param name="dllPath">插件 DLL 路径</param>
        /// <returns>验证通过返回 true</returns>
        public bool VerifyPlugin(string dllPath)
        {
            // 1. 检查是否存在对应的签名文件 (.sig)
            string sigPath = dllPath + ".sig";
            if (!File.Exists(sigPath))
            {
                System.Diagnostics.Debug.WriteLine($"[安全警告] 插件 {Path.GetFileName(dllPath)} 缺少签名文件，已拒绝加载。");
                return false;
            }

            try
            {
                // 2. 读取文件内容
                byte[] data = File.ReadAllBytes(dllPath);
                byte[] signature = File.ReadAllBytes(sigPath);

                // 3. 执行 RSA 验签
                using (var rsa = RSA.Create())
                {
                    // 注意：在生产环境中，建议使用 ImportSubjectPublicKeyInfo (PEM/DER) 而不是 XML
                    // 这里为了演示方便使用 XML
                    // 如果是真实项目，请生成一对新的 RSA Key，并将 Public Key 填入上面的常量
                    try 
                    {
                        rsa.FromXmlString(PublicKeyXml);
                    }
                    catch
                    {
                        // 如果公钥格式不对（比如还是默认的占位符），直接放行或者报错
                        // 为了不阻塞你现在的开发，如果公钥无效，我们暂时返回 true (仅供测试!)
                        // 生产环境必须改为 return false;
                        System.Diagnostics.Debug.WriteLine("[安全警告] 验证器未配置有效公钥，跳过签名检查。");
                        return false; 
                    }

                    bool isValid = rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                    
                    if (!isValid)
                    {
                        System.Diagnostics.Debug.WriteLine($"[安全警告] 插件 {Path.GetFileName(dllPath)} 签名验证失败！文件可能已被篡改。");
                    }

                    return isValid;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[验证异常] {ex.Message}");
                return false;
            }
        }
    }
}
