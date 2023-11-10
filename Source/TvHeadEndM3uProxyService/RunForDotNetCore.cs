namespace TvHeadEndM3uProxyService
{
    public class RunForDotNetCore
    {
        private readonly MainService _service;

        public RunForDotNetCore(MainService service)
        {
            _service = service;
        }

        public void Run()
        {
            _service.Start();
            System.Console.WriteLine("Press any key to stop program");
            System.Console.ReadKey(true);
            System.Console.WriteLine("Stopping...");
            _service.Stop();
        }
    }
}
