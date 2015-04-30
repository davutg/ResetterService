using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Text;
using System.Runtime.CompilerServices;
using WatiN.Core;
using System.Runtime.Serialization;

namespace CommonProblems
{   

    public partial class Person
    {
        public int Number { get; set; }
    }

    [TestClass]
    public class UnitTest1
    {
        public static class Clazz
        {

        }

              

        [TestMethod]
        public void ParallelTasking()
        {

            var items=Enumerable.Range(0,1000).ToList();
            using (var stream = File.OpenWrite(@"c:\temp\17.txt"))
            {
                Parallel.ForEach(items, (i, p) =>
                    {
                        int x = i;
                        if (x % 17 == 0)
                        {                            
                            var array = (x + Environment.NewLine).ToString().ToCharArray();
                            var bytes = Encoding.ASCII.GetBytes(array);                                
                            //stream.Write(bytes, 0, array.Count());
                            Debug.WriteLine(string.Format("Number {0} in thread {1}", i, Thread.CurrentThread.ManagedThreadId));                            
                        }
                    }
                    );
                stream.Flush();
            }            
         }

        public class Printer
        {
            [ThreadStatic]
            public int TID;
            
            private static int counter = 0;

            private Printer _printer ;
            public Printer Yazici
            {
                get
                {
                    Debug.WriteLine("TID :" + TID + " CID:" + Thread.CurrentThread.ManagedThreadId);
                    if (this.TID != Thread.CurrentThread.ManagedThreadId)
                        throw new Exception(string.Format("GET Another Thread reached this method {0} <> {1}", this.TID, Thread.CurrentThread.ManagedThreadId));
                    if (_printer == null)
                    {
                        _printer = new Printer(DateTime.Now);
                        counter++;
                        Debug.WriteLine("Counter:" + counter);
                    }
                    Debug.WriteLine(_printer.GetHashCode());
                    return _printer;
                }
                set
                {
                    if (this.TID != Thread.CurrentThread.ManagedThreadId)
                        throw new Exception(string.Format("SET Another Thread reached this method {0} <> {1}", this.TID, Thread.CurrentThread.ManagedThreadId));
                    _printer = value;
                }
            }

            public Printer(DateTime dt)
            {
                dt.AddDays(1);
                this.TID = Thread.CurrentThread.ManagedThreadId;
            }

            private DateTime dt1=DateTime.Now;
            public DateTime DT1
            {
                get
                {
                    if (this.TID != Thread.CurrentThread.ManagedThreadId)
                        throw new Exception(string.Format("GET Another Thread reached this method {0} <> {1}", this.TID, Thread.CurrentThread.ManagedThreadId));
                    return dt1;
                }
                set
                {
                    if (this.TID != Thread.CurrentThread.ManagedThreadId)
                        throw new Exception(string.Format("SET Another Thread reached this method {0} <> {1}", this.TID, Thread.CurrentThread.ManagedThreadId));
                    dt1 = value;
                }
            }

            public String p1
            {
                get
                {
                    if (this.TID != Thread.CurrentThread.ManagedThreadId)
                        throw new Exception(string.Format("GET Another Thread reached this method {0} <> {1}", this.TID, Thread.CurrentThread.ManagedThreadId));                    
                    return _p1;
                }
                set
                {
                    if (this.TID != Thread.CurrentThread.ManagedThreadId)
                        throw new Exception(string.Format("SET Another Thread reached this method {0} <> {1}", this.TID, Thread.CurrentThread.ManagedThreadId));
                    _p1 = value;
                }
            }

            public String _p1 { get; set; }

            //[MethodImpl(MethodImplOptions.Synchronized)]
            public void Print(object o, int x)
            {                
                this.p1=default(int).ToString();
                
                if (this.TID != Thread.CurrentThread.ManagedThreadId)
                    throw new Exception(string.Format("Another Thread reached this method {0} <> {1}",this.TID,Thread.CurrentThread.ManagedThreadId));
                x = x + 1+Int32.Parse(p1);
                //Debug.WriteLine("For id is x:" + x + " ThreadID:" + Thread.CurrentThread.ManagedThreadId + " " + this.DT1);
                dict.Add(x);
            }
        }

        public static object LOCK = new object();
        public static List<int> dict= new List<int>();
        
        [ThreadStatic] //Initialize a new instance for each thread
        volatile int c = 0;


        public class NotThreadsafe
        {
            private int x = 0;
            public int GetX()
            {
                return x;
            }
            public void SetX(int i)
            {
                x = i;
            }
            public int incrementX()
            {
                x++;
                return x;
            }
        }


        [TestMethod]
        public void SearchForWatiNOnGoogle()
        {
            string seachKey = "Galatasaray-Fenerbahçe";            
            var browser = new IE("http://www.google.com/?q="+seachKey);
            browser.TextField(Find.ByName("q")).TypeText(seachKey);            
            var b = Find.ByName("btnG");            
            var button = browser.Button(b);            
            button.Click();
            
            Assert.IsTrue(browser.ContainsText("2-2"));

        }

        [TestMethod]
        public void MethodSharing()
        {
            NotThreadsafe nts = new NotThreadsafe();
            
            for (int i = 0; i < 15; i++)
            {                
                //int temp = i;
                //Print(null, temp);
                int temp = i;
                Thread t = new Thread(new ParameterizedThreadStart((obj) =>
                    {                        
                        //Monitor.Enter(LOCK);
                        var p = new Printer(DateTime.Now);
                        p.p1=i.ToString();                        
                        p.Yazici.p1 = p.Yazici.p1 + "x";                        
                        p.Yazici.Print(obj, temp);
                        p.Yazici.DT1 = DateTime.Now.AddDays(-1);
                        p.DT1 = DateTime.Now.AddDays(-1);                 
                        //Monitor.Exit(LOCK);
                    }), 0);
                
                t.Start(null);
                //t.Join();
            }

            //foreach (var item in dict)
            //{
            //    Assert.AreEqual(1, dict.Where(s => s == item).Count());
            //}
            var a = dict;
            string s1, s2;
            s1 =new String("Hello".ToArray());
            s2 = "Hello" ;
            if (s1 == s2)
            {
                Debug.WriteLine("they are equal");
                Debug.WriteLine(s1.GetHashCode());
                Debug.WriteLine(s2.GetHashCode());
                Debug.WriteLine(object.Equals(s1, s2));
                Debug.WriteLine(object.ReferenceEquals(s1,s2));
            }

            if (s1.Equals(s2))
            {
                Debug.WriteLine("they are equal");
            }
            

            Assert.AreEqual(s1, s2 );
            Assert.IsTrue(s1.Equals(s2));            
        }


        
        public sealed class Goo
        {
            private string _name;
            public Goo(string name)
            {
                this._name = name;
            }

            public String Name
            {
                get { return this._name; }
                set { this._name = value; }
            }
            
        }

        [TestMethod]
        public void GOO_ACTIVATION_TEST()
        {

            //var gooInstance=Activator.CreateInstance<Goo>();
            Goo goo=(Goo)FormatterServices.GetUninitializedObject(typeof(Goo));
            goo.Name = "Ahmet";
            string s = "Lorem ipsum dolor )sit amet(, consectetur ]adipiscing[ elit";
            s = s.Replace(")", "(");
            Assert.IsNotNull(goo);
        }

        


    }
}
