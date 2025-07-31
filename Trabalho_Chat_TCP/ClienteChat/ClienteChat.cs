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

        TextBox txtMensagens;
        Button btnConectar;
        TcpClient cliente;
        NetworkStream stream;
        Thread threadReceber;

        ListBox lstUsuarios;

        TcpListener servidorPrivado;
        string apelido;
        int portaPrivada;

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

            lstUsuarios = new ListBox { Dock = DockStyle.Top, Height = 120 };

            btnConectar = new Button { Text = "Conectar", Dock = DockStyle.Top, Height = 30 };

            var painelBroadcast = new Panel { Dock = DockStyle.Bottom, Height = 30 };
            TextBox txtBroadcast = new TextBox { Dock = DockStyle.Fill };
            Button btnBroadcast = new Button { Text = "Broadcast", Dock = DockStyle.Right, Width = 90 };
            painelBroadcast.Controls.Add(txtBroadcast);
            painelBroadcast.Controls.Add(btnBroadcast);

            var painelSuperior = new Panel { Dock = DockStyle.Top, Height = 30 };
            Label lblUsuarios = new Label { Text = "Usuarios Conectados: ?", AutoSize = true, Dock = DockStyle.Left };
            Button btnListar = new Button { Text = "Listar", Width = 80, Dock = DockStyle.Right };
            Button btnAtualizar = new Button { Text = "Atualizar", Width = 80, Dock = DockStyle.Right };
            painelSuperior.Controls.Add(lblUsuarios);
            painelSuperior.Controls.Add(btnListar);
            painelSuperior.Controls.Add(btnAtualizar);

            Controls.Add(txtMensagens);
            Controls.Add(lstUsuarios);
            Controls.Add(btnConectar);
            Controls.Add(painelSuperior);
            Controls.Add(painelBroadcast);

            // Definir porta privada dinâmica (usar 0 para escolher automaticamente)
            servidorPrivado = new TcpListener(IPAddress.Any, 0);
            servidorPrivado.Start();
            portaPrivada = ((IPEndPoint)servidorPrivado.LocalEndpoint).Port;

            // Conectar ao servidor principal
            cliente = new TcpClient();
            cliente.Connect(ipServidor, porta);
            stream = cliente.GetStream();

            // Enviar apelido e porta privada na primeira mensagem
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
                        var ipRemoto = ((IPEndPoint)clientePrivado.Client.RemoteEndPoint).Address.ToString();
                        string chave = $"{ipRemoto}:{portaPrivada}";

                        if (!janelasPrivadas.ContainsKey(chave))
                        {
                            var chatPrivado = new JanelaChatPrivado(apelido, ipRemoto, portaPrivada);
                            chatPrivado.AssociarCliente(clientePrivado);

                            chatPrivado.FormClosed += (a, b) => janelasPrivadas.Remove(chave);

                            janelasPrivadas[chave] = chatPrivado;
                            chatPrivado.Show();
                        }
                        else
                        {
                            clientePrivado.Close(); // Ignora duplicado
                        }


                    }));
                }
            });
            threadServidorPrivado.IsBackground = true;
            threadServidorPrivado.Start();

            btnBroadcast.Click += (s, e) => EnviarBroadcast(apelido, txtBroadcast);
            btnAtualizar.Click += (s, e) => AtualizarContador(lblUsuarios);
            btnListar.Click += (s, e) => ListarUsuarios();
            btnConectar.Click += (s, e) => ConectarPrivado();
        }

        void ConectarPrivado()
        {
            if (lstUsuarios.SelectedItem == null)
            {
                MessageBox.Show("Selecione um usuário para conectar.");
                return;
            }

            string item = lstUsuarios.SelectedItem.ToString(); // Exemplo: "Maria (192.168.0.10:3005)"
            int idxIni = item.LastIndexOf('(');
            int idxFim = item.LastIndexOf(')');
            if (idxIni < 0 || idxFim < 0 || idxFim <= idxIni)
            {
                MessageBox.Show("Formato inválido do usuário selecionado.");
                return;
            }

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

            var chatPrivado = new JanelaChatPrivado(apelido, ipDestino, portaDestino);
            chatPrivado.Show();
        }

        void ListarUsuarios()
        {
            byte[] buffer = Encoding.UTF8.GetBytes("/lista");
            stream.Write(buffer, 0, buffer.Length);
        }

        void AtualizarContador(Label lbl)
        {
            byte[] buffer = Encoding.UTF8.GetBytes("/count");
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

                    if (msg.StartsWith("Usuarios Conectados:"))
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
            var linhas = resposta.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var linha in linhas)
            {
                if (linha.StartsWith("- "))
                {
                    lstUsuarios.Items.Add(linha.Substring(2).Trim());
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

    public class JanelaChatPrivado : Form
    {
        TextBox txtMensagens, txtEntrada;
        Button btnEnviar;
        TcpClient cliente;
        NetworkStream stream;
        Thread threadReceber;
        string apelido;

        public JanelaChatPrivado(string apelido, string ipDestino, int porta)
        {
            this.apelido = apelido;

            Text = $"Chat Privado - {apelido} -> {ipDestino}:{porta}";
            Width = 400;
            Height = 300;

            txtMensagens = new TextBox
            {
                Multiline = true,
                Dock = DockStyle.Top,
                Height = 200,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical
            };

            txtEntrada = new TextBox { Dock = DockStyle.Fill };
            btnEnviar = new Button { Text = "Enviar", Dock = DockStyle.Right, Width = 75 };

            var painelInferior = new Panel { Dock = DockStyle.Bottom, Height = 30 };
            painelInferior.Controls.Add(txtEntrada);
            painelInferior.Controls.Add(btnEnviar);

            Controls.Add(txtMensagens);
            Controls.Add(painelInferior);

            cliente = new TcpClient();
            cliente.Connect(ipDestino, porta);
            stream = cliente.GetStream();

            threadReceber = new Thread(() => ReceberMensagens());
            threadReceber.IsBackground = true;
            threadReceber.Start();

            btnEnviar.Click += (s, e) => EnviarMensagem();
        }

        public void AssociarCliente(TcpClient clienteExistente)
        {
            cliente = clienteExistente;
            stream = cliente.GetStream();

            threadReceber = new Thread(() => ReceberMensagens());
            threadReceber.IsBackground = true;
            threadReceber.Start();
        }


        void EnviarMensagem()
        {
            string msg = txtEntrada.Text.Trim();
            if (msg == "") return;

            string conteudo = $"{apelido}: {msg}";
            byte[] dados = Encoding.UTF8.GetBytes(conteudo);
            stream.Write(dados, 0, dados.Length);
            txtMensagens.AppendText("Eu: " + msg + Environment.NewLine);
            txtEntrada.Clear();
        }

        void ReceberMensagens()
        {
            try
            {
                byte[] buffer = new byte[1024];
                while (true)
                {
                    int lidos = stream.Read(buffer, 0, buffer.Length);
                    if (lidos == 0) break;
                    string msg = Encoding.UTF8.GetString(buffer, 0, lidos);
                    Invoke((MethodInvoker)(() => txtMensagens.AppendText($"[Privado] {msg}{Environment.NewLine}")));

                }
            }
            catch { }
            finally
            {
                cliente.Close();
            }
        }
    }
}
