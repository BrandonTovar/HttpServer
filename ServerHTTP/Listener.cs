using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Worker;

namespace ServerHTTP
{
    public interface HandlerContext
    {
        void Add(HttpListenerContext Conn);
        void Remove(HttpListenerContext Conn);
    }
    public enum StateListenerEnum
    {
        Listening,
        Pause,
        Finished
    }
    public enum InfoListenerEnum
    {
        Prefixes,
        Ports,
        HostFrom
    }

    // Clase abstracta especializada para la creacion de servidor http para las consultas,
    // se mantiene en una clase aparte para su posterior modificacion.
    public abstract class RequestCFDI
    {
        private static HttpListener Server = new HttpListener();
        private IPAddress[] IPlocal;
        private List<short> PORT = new List<short>();
        private HttpListenerPrefixCollection Prefixes;
        private List<string> HostFrom = new List<string>();
        public StateListenerEnum State = StateListenerEnum.Pause;
        public Dictionary<InfoListenerEnum, object> Info = new Dictionary<InfoListenerEnum, object>();
        public List<object> Handlers = new List<object>();

        public void Initialize(short[] PORTs, string[] HostAddress)
        {
            Prefixes = Server.Prefixes;
            // Obtenemos la ip local
            IPlocal = Dns.Resolve(Dns.GetHostName()).AddressList;
            // Asignamos los prefijos para el servidor
            AddPort(PORTs);
            AddFrom(HostAddress);
            // Asignamos los datos para la info
            Info.Add(InfoListenerEnum.HostFrom, HostFrom);
            Info.Add(InfoListenerEnum.Ports, PORT);
            Info.Add(InfoListenerEnum.Prefixes, Prefixes);
        }
        public void Start()
        {
            if (Server != null)
                try {
                    Server.Start();
                    State = StateListenerEnum.Listening;
                    GetContext();
                }
                catch (Exception e) {
                    Server.Abort();
                    State = StateListenerEnum.Finished;
                    Console.WriteLine(e.Message);
                }
            
        }
        public void Stop()
        {
            if (Server != null) try { Server.Abort(); } catch { }
            State = StateListenerEnum.Pause;
        }
        public void AddPort(short[] PORTs)
        {
            foreach (short port in PORTs)
            {
                if (!PORT.Contains(port))
                {
                    PORT.Add(port);
                    foreach (IPAddress ip in IPlocal)
                    {
                        string Prefix = "http://" + ip.ToString() + ":" + port + "/";
                        Prefixes.Add(Prefix); 
                    }                    
                }
            }
        }
        public void RemovePort(short[] PORTs)
        {
            foreach (short port in PORTs)
            {
                if (PORT.Contains(port))
                {
                    PORT.Remove(port);
                    foreach (IPAddress ip in IPlocal)
                    {
                        string Prefix = "http://" + ip.ToString() + ":" + port + "/";
                        try { Prefixes.Remove(Prefix); }
                        catch { }
                        Console.WriteLine(Prefix);
                    }
                    
                }
            }
        }
        public void AddFrom(string[] HostAddress)
        {
            foreach (string host in HostAddress)
                if (!HostFrom.Contains(host)) HostFrom.Add(host);
        }
        public void RemoveFrom(string[] HostAddress)
        {
            foreach (string host in HostAddress)
                if (HostFrom.Contains(host)) HostFrom.Remove(host);
        }
        private void GetContext()
        {
            Console.WriteLine("Inicio el servicio de recepcion");
            for (int i = 0; State == StateListenerEnum.Listening; i++)
            {
                if (i >= Handlers.Count()) i = 0;
                if (Server.Prefixes.Count() >= 1)
                {
                    Console.WriteLine("Esperando conexion");
                    HttpListenerContext Context = Server.GetContext();
                    Console.WriteLine(Context.Request.RemoteEndPoint);
                    if (Handlers.Count() >= 1)
                    {
                        Console.WriteLine("Send to worker");
                        if (HostFrom.Contains(Context.Request.RemoteEndPoint.ToString()))
                            ((HandlerContext)Handlers[i]).Add(Context);
                        else Context.Response.Close();
                    }
                    else Context.Response.Close();
                }
                
            }
        }
    }
}
