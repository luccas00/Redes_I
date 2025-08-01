using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Chat_TCP
{
    public class ClienteChat : Form
    {
        
        private static Dictionary<string, JanelaChatPrivado> janelasPrivadas = new();

        private Label lblUsuarios;

        TextBox txtMensagens;
        Button btnConectar;
        TcpClient cliente;
        NetworkStream stream;
        Thread threadReceber;

        ListBox lstUsuarios;

        TcpListener servidorPrivado;
        string apelido;
        int portaPrivada;

        bool recebendoListaUsuarios = false;

        public ClienteChat(string apelido, string ipServidor, int porta)
        {
            this.apelido = apelido;

            Text = $"Chat - {apelido}";
            Width = 600;
            Height = 500;

            txtMensagens = new TextBox
            {
                Multiline = true,
                Dock = DockStyle.Top,
                Height = 200,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical
            };

            lstUsuarios = new ListBox
            {
                Dock = DockStyle.Top,
                Height = 120,
                Enabled = true,
                SelectionMode = SelectionMode.One
            };
            lstUsuarios.DoubleClick += (s, e) => ConectarPrivado();

            btnConectar = new Button { Text = "Conectar Chat Privado", Dock = DockStyle.Top, Height = 30 };

            var painelBroadcast = new Panel { Dock = DockStyle.Bottom, Height = 30 };
            TextBox txtBroadcast = new TextBox { Dock = DockStyle.Fill };
            Button btnBroadcast = new Button { Text = "Broadcast", Dock = DockStyle.Right, Width = 90 };
            painelBroadcast.Controls.Add(txtBroadcast);
            painelBroadcast.Controls.Add(btnBroadcast);

            var painelConectar = new Panel { Dock = DockStyle.Top, Height = 30 };
            painelConectar.Controls.Add(btnConectar);

            var painelSuperior = new Panel { Dock = DockStyle.Top, Height = 30 };
            Button btnListar = new Button { Text = "Listar Usuarios", Width = 150, Dock = DockStyle.Right };
            painelSuperior.Controls.Add(btnListar);

            Controls.Add(txtMensagens);
            Controls.Add(lstUsuarios);
            Controls.Add(painelSuperior);
            Controls.Add(painelConectar);
            Controls.Add(painelBroadcast);

            // Porta privada dinâmica
            servidorPrivado = new TcpListener(IPAddress.Any, 0);
            servidorPrivado.Start();
            portaPrivada = ((IPEndPoint)servidorPrivado.LocalEndpoint).Port;

            // Conectar servidor principal
            cliente = new TcpClient();
            cliente.Connect(ipServidor, porta);
            stream = cliente.GetStream();

            // Enviar apelido e porta privada
            string dadosConexao = $"{apelido};{portaPrivada}";
            byte[] dadosBytes = Encoding.UTF8.GetBytes(dadosConexao);
            stream.Write(dadosBytes, 0, dadosBytes.Length);

            threadReceber = new Thread(() => ReceberMensagens());
            threadReceber.IsBackground = true;
            threadReceber.Start();

            Thread threadServidorPrivado = new Thread(() =>
            {
                while (true)
                {
                    TcpClient clientePrivado = servidorPrivado.AcceptTcpClient();
                    Invoke((MethodInvoker)(() =>
                    {
                        // Lê o apelido enviado pelo cliente remoto
                        NetworkStream streamPrivado = clientePrivado.GetStream();
                        byte[] bufferApelido = new byte[1024];
                        int lidos = streamPrivado.Read(bufferApelido, 0, bufferApelido.Length);
                        string apelidoRemoto = Encoding.UTF8.GetString(bufferApelido, 0, lidos);

                        // Extrai IP e porta do cliente que iniciou a conexão
                        var remoteEndPoint = (IPEndPoint)clientePrivado.Client.RemoteEndPoint;
                        int portaRemota = remoteEndPoint.Port;

                        var ipRemoto = ((IPEndPoint)clientePrivado.Client.RemoteEndPoint).Address.ToString();

                        if (ipRemoto == "127.0.0.1" || ipRemoto == "::1")
                        {
                            ipRemoto = ObterIpLocalDaRede(); // Função utilitária
                        }


                        string chave = $"{ipRemoto}:{portaRemota}";

                        if (!janelasPrivadas.ContainsKey(chave))
                        {
                            var chatPrivado = new JanelaChatPrivado(apelido, apelidoRemoto, ipRemoto, portaRemota);
                            chatPrivado.AssociarCliente(clientePrivado);

                            chatPrivado.FormClosed += (a, b) => janelasPrivadas.Remove(chave);

                            janelasPrivadas[chave] = chatPrivado;
                            chatPrivado.Show();
                        }
                        else
                        {
                            clientePrivado.Close();
                        }

                    }));
                }
            });
            threadServidorPrivado.IsBackground = true;
            threadServidorPrivado.Start();

            btnBroadcast.Click += (s, e) => EnviarBroadcast(apelido, txtBroadcast);
            btnListar.Click += (s, e) => ListarUsuarios();
            btnConectar.Click += (s, e) => ConectarPrivado();
        }

        public static string ObterIpLocalDaRede()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                {
                    return ip.ToString();
                }
            }
            return "127.0.0.1";
        }


        void ConectarPrivado()
        {
            if (lstUsuarios.SelectedItem == null)
            {
                MessageBox.Show("Selecione um usuário para conectar.");
                return;
            }

            string item = lstUsuarios.SelectedItem.ToString(); // Exemplo: "João (192.168.0.10:3005)"
            int idxIni = item.LastIndexOf('(');
            int idxFim = item.LastIndexOf(')');
            if (idxIni < 0 || idxFim < 0 || idxFim <= idxIni)
            {
                MessageBox.Show("Formato inválido do usuário selecionado.");
                return;
            }

            string apelidoDestino = item.Substring(0, idxIni).Trim();
            string endereco = item.Substring(idxIni + 1, idxFim - idxIni - 1);
            var partes = endereco.Split(':');
            if (partes.Length != 2)
            {
                MessageBox.Show("Endereço inválido.");
                return;
            }

            string ipDestino = partes[0];
            if (!int.TryParse(partes[1], out int portaDestino))
            {
                MessageBox.Show("Porta inválida.");
                return;
            }

            var chatPrivado = new JanelaChatPrivado(apelido, apelidoDestino, ipDestino, portaDestino);
            chatPrivado.Show();
        }

        void ListarUsuarios()
        {
            byte[] buffer = Encoding.UTF8.GetBytes("/lista");
            stream.Write(buffer, 0, buffer.Length);
        }

        void EnviarBroadcast(string apelido, TextBox txt)
        {
            string mensagem = txt.Text.Trim();
            if (mensagem == "") return;

            string conteudo = $"[Broadcast] {apelido}: {mensagem}";
            byte[] buffer = Encoding.UTF8.GetBytes(conteudo);
            stream.Write(buffer, 0, buffer.Length);
            txt.Clear();
        }

        void ReceberMensagens()
        {
            try
            {
                byte[] buffer = new byte[1024];
                while (true)
                {
                    int bytesLidos = stream.Read(buffer, 0, buffer.Length);
                    if (bytesLidos == 0) break;

                    string msg = Encoding.UTF8.GetString(buffer, 0, bytesLidos);

                    // Verifica se mensagem é lista de usuários
                    if (msg.Contains(";") && msg.Contains(".") && msg.Contains("\n"))
                    {
                        AtualizarListaUsuarios(msg);
                    }
                    else
                    {
                        Invoke((MethodInvoker)(() => txtMensagens.AppendText(msg + Environment.NewLine)));
                    }
                }
            }
            catch { }
            finally
            {
                cliente.Close();
            }
        }



        void AtualizarListaUsuarios(string resposta)
        {
            lstUsuarios.Items.Clear();

            // A resposta provavelmente vem com várias linhas nesse formato: apelido;ip;porta
            var linhas = resposta.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var linha in linhas)
            {
                // Ignora linhas que não tenham ';' (evita lixo)
                if (!linha.Contains(";")) continue;

                var partes = linha.Split(';');
                if (partes.Length == 3)
                {
                    string apelido = partes[0];
                    string ip = partes[1];
                    string porta = partes[2];
                    lstUsuarios.Items.Add($"{apelido} ({ip}:{porta})");
                }
            }
        }



        [STAThread]
        public static void Main(string[] args)
        {
            string apelido = args.Length > 0 ? args[0] : "Anonimo";
            string ipServidor = args.Length > 1 ? args[1] : "127.0.0.1";
            int porta = 1998;

            Application.EnableVisualStyles();
            Application.Run(new ClienteChat(apelido, ipServidor, porta));
        }
    }
}

