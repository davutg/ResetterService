using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ResetterService
{
    public class Clazz
    {
        public string ClassName { get; set; }
        public SortedList ConstructorParameters = new SortedList();

        public void AddArgument(string arg)
        {
            ConstructorParameters.Add(ConstructorParameters.Count, arg);
        }

        public Clazz(string name)
        {
            this.ClassName = name;
        }
    }

    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            List<ResertterServiceBase> services;

            if (args == null || string.IsNullOrEmpty(args.FirstOrDefault()))
            {
                services = new List<ResertterServiceBase>() { new RestarterService() };
            }
            else
            {

                List<Clazz> classNames = new List<Clazz>();
                
                string paramsLine = string.Empty;
                Regex regex = new Regex("-([C|p])'(.+?)'");
                args.ToList().ForEach((s => paramsLine += " " + s));
                MatchCollection matches = regex.Matches(paramsLine);
                var enumerator=matches.GetEnumerator();

                Clazz clazz=null;
	            while (enumerator.MoveNext())
	            {
                    if (enumerator.Current is Match)
                    {
                        Match match = (Match)enumerator.Current;
                        if(match.Success)
                        {
                            Console.WriteLine(match.Value);
                            if (match.Groups[1].Value == "C")
                            {
                                clazz=new Clazz(match.Groups[2].Value);
                                classNames.Add(clazz);
                            }
                            if(match.Groups[1].Value == "p")
                            {
                                if(clazz!=null)
                                clazz.AddArgument(match.Groups[2].Value);
                            }
                        }
                    }
                    
	            }

                services = new List<ResertterServiceBase>();
                foreach (var clazzInstance in classNames)
	            {
                    try
                    {
                        Type t = Type.GetType(System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + "." + clazzInstance.ClassName);
                        
                        int parameterCount = clazzInstance.ConstructorParameters.Count;
                        List<Type> allParameterVariables = new List<Type>();
                        List<string> allParameters = new List<string>(parameterCount);
                        for(int i=0;i<parameterCount;i++)
                        {
                            allParameterVariables.Add(typeof(string));
                            allParameters.Add(clazzInstance.ConstructorParameters.GetByIndex(i).ToString());
                        }

                        var ctor = t.GetConstructor(allParameterVariables.Count == 0 ? null : allParameterVariables.ToArray());

                        if (ctor != null)
                        {
                            var serviceInstance = ctor.Invoke(allParameters.ToArray());
                            if (serviceInstance is ResertterServiceBase)
                                services.Add((ResertterServiceBase)serviceInstance);
                        }
                        else
                        { 
                            Console.WriteLine(string.Format("No ctor found for this {0}",clazzInstance));
                        }
                    }
                    catch (TypeLoadException e)
                    {
                        Console.WriteLine(e.Message);
                        Console.ReadLine();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        Console.ReadLine();
                    }
                    
                    
                   
	            }                
            }


            if (Environment.UserInteractive)
            {
                foreach (var service in services)
                {
                    new Thread(new ParameterizedThreadStart((a) =>
                    {
                        service.FireOnStart(null); //TODO: add sevice argumens as parameter
                        Console.WriteLine(string.Format("Service Started : {0} ThreadID:{1}", service.DisplayName??service.ToString(), Thread.CurrentThread.GetHashCode() ));
                        Console.Read();
                        Console.WriteLine("Press any key to stop program");
                        service.FireOnStop();
                        
                    }
                   )).Start();
                    
                }
                
              

            }
            else
            {
                ServiceBase.Run(services.ToArray());
            }

        }
    }
}
