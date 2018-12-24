using System;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ShutMiWiFiDown
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string password=null, host=null;
            var configPath = typeof(Program).Assembly.Location + ".xml";
            try
            {
                var xe = XElement.Load(configPath);
                host = xe.Element("Host").Value;
                password = xe.Element("Password").Value;
                if(host == "http://hostaddress" || password == "your_login_password")
                    Error("请先配置路由器管理页面地址和登录密码");

                var user = User.Create(password, host);
                Console.Write("登录...");
                await user.LoginAsync();
                Console.WriteLine("成功!");
                Console.WriteLine("按下任意键断开PPPoE");
                Console.ReadKey();
                await user.StopPppoeAsync();
                Exit(0);
            }
            catch(System.IO.FileNotFoundException)
            {
                new XElement("Config",
                    new XElement("Host", "http://hostaddress"),
                    new XElement("Password", "your_login_password"))
                    .Save(configPath);
                Error("请先配置路由器管理页面地址和登录密码");
            }
        }

        static void Error(string message)
        {
            Console.WriteLine(message);
            Exit(-1);
        }

        static void Exit(int code)
        {
            Console.WriteLine("按下任意键退出...");
            Console.ReadKey();
            Environment.Exit(code);
        }
    }
}
