namespace LABORATORIJSKEOOAD
{
    internal class Program
    {
        public delegate void MojDelegat(int test);
        public static event MojDelegat MojEvent;
        static void Main(string[] args)
        {
            MojEvent += new MojDelegat((int test) => Console.WriteLine("Test" + test.ToString()));
            MojEvent.Invoke(5);
        }
        
       
    }
}
