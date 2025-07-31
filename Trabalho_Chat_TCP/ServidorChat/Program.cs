using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;

namespace Chat_TCP
{
    class Program
    {
        static TcpListener listener;
        static List<(TcpClient cliente, string apelido, string ip, int portaPrivada)> clientes = new();
        static object locker = new();

        static void Main()
        {
            int porta = 1998;
            IPAddress ip = IPAddress.Any;
            listener = new TcpListener(ip, porta);
            listener.Start();
            Console.WriteLine($"Servidor ouvindo na porta {porta}");
            Console.WriteLine($"Servidor ouvindo de todos os IPs");

            while (true)
            {
                TcpClient cliente = listener.AcceptTcpClient();
                NetworkStream stream = cliente.GetStream();
                byte[] buffer = new byte[1024];

                int bytesLidos = stream.Read(buffer, 0, buffer.Length);
                string dados = Encoding.UTF8.GetString(buffer, 0, bytesLidos);

                // Espera formato "apelido;porta"
                var partes = dados.Split(';');
                if (partes.Length != 2 || !int.TryParse(partes[1], out int portaPrivada))
                {
                    Console.WriteLine("Dados inválidos do cliente, desconectando.");
                    cliente.Close();
                    continue;
                }

                string apelido = partes[0];
                string ipCliente = ((IPEndPoint)cliente.Client.RemoteEndPoint).Address.ToString();

                lock (locker)
                {
                    clientes.Add((cliente, apelido, ipCliente, portaPrivada));
                    Console.WriteLine($"Novo usuário conectado: {apelido} ({ipCliente}:{portaPrivada})");
                    Console.WriteLine($"Total de usuários: {clientes.Count}");
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

                    if (mensagem == "/count")
                    {
                        int total;
                        lock (locker)
                            total = clientes.Count;

                        string resposta = $"Usuarios Conectados: {total}";
                        byte[] respostaBuffer = Encoding.UTF8.GetBytes(resposta);
                        stream.Write(respostaBuffer, 0, respostaBuffer.Length);
                        continue;
                    }
                    else if (mensagem == "/lista")
                    {
                        StringBuilder sb = new();
                        sb.AppendLine("Usuarios Conectados:");
                        lock (locker)
                        {
                            foreach (var c in clientes)
                                sb.AppendLine($"- {c.apelido} ({c.ip}:{c.portaPrivada})");
                        }

                        byte[] resposta = Encoding.UTF8.GetBytes(sb.ToString());
                        stream.Write(resposta, 0, resposta.Length);
                        continue;
                    }

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
                    var clienteRemover = clientes.Find(c => c.cliente == cliente);
                    if (clienteRemover.cliente != null)
                        clientes.Remove(clienteRemover);

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
                    if (c.cliente != remetente)
                    {
                        try
                        {
                            NetworkStream stream = c.cliente.GetStream();
                            stream.Write(dados, 0, dados.Length);
                        }
                        catch
                        {
                            // Cliente desconectado, opcional remover da lista
                        }
                    }
                }
            }
        }
    }
}
