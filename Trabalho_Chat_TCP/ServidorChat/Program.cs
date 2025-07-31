
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;

namespace Chat_TCP;

class Program
{
    static TcpListener listener;
    static List<TcpClient> clientes = new();
    static object locker = new();

    static void Main()
    {
        int porta = 1998;
        IPAddress ip = IPAddress.Any;
        listener = new TcpListener(ip, porta);
        listener.Start();
        Console.WriteLine($"Servidor ouvindo na porta {porta}...");
        Console.WriteLine($"Servidor ouvindo no IP {ip}...");
        Console.WriteLine($"Servidor IPv4 {ip.MapToIPv4}...");
        Console.WriteLine($"Servidor IPv6 {ip.MapToIPv6}...");

        while (true)
        {
            TcpClient cliente = listener.AcceptTcpClient();
            lock (locker)
            {
                clientes.Add(cliente);
                Console.WriteLine($"Novo usuário conectado. Total de usuários: {clientes.Count}");
            }

            Thread thread = new(() => AtenderCliente(cliente));
            thread.Start();
        }
    }

    static void AtenderCliente(TcpClient cliente)
    {
        try
        {
            NetworkStream stream = cliente.GetStream();
            byte[] buffer = new byte[1024];
            int bytesLidos;

            while ((bytesLidos = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                string mensagem = Encoding.UTF8.GetString(buffer, 0, bytesLidos);
                Console.WriteLine($"Mensagem recebida: {mensagem}");
                Broadcast(mensagem, cliente);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro: " + ex.Message);
        }
        finally
        {
            lock (locker)
            {
                clientes.Remove(cliente);
                Console.WriteLine($"Usuário desconectado. Total de usuários: {clientes.Count}");
            }
            cliente.Close();
        }
    }

    static void Broadcast(string mensagem, TcpClient remetente)
    {
        byte[] dados = Encoding.UTF8.GetBytes(mensagem);

        lock (locker)
        {
            foreach (var c in clientes)
            {
                if (c != remetente)
                {
                    try
                    {
                        NetworkStream stream = c.GetStream();
                        stream.Write(dados, 0, dados.Length);
                    }
                    catch
                    {
                        // Cliente desconectado
                    }
                }
            }
        }
    }
}
