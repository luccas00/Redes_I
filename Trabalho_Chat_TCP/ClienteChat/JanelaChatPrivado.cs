using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using System.IO;

namespace Chat_TCP
{
    public class JanelaChatPrivado : Form
    {
        TextBox txtMensagens, txtEntrada;
        Button btnEnviar;
        TcpClient cliente;
        NetworkStream stream;
        Thread threadReceber;

        string apelidoLocal;
        string apelidoDestino;
        string ipDestino;
        int portaDestino;

        public string ApelidoRemoto;

        static readonly string logPath = "client_log.txt";
        static readonly string errorLogPath = "client_errors.csv";

        static void Log(string msg) => File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}{Environment.NewLine}");
        static void LogError(Exception ex)
        {
            string msg = ex.Message.Replace("\"", "\"\"");
            File.AppendAllText(errorLogPath, $"\"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\",\"{msg}\"{Environment.NewLine}");
        }

        public JanelaChatPrivado(string apelidoLocal, string apelidoDestino, string ipDestino, int portaDestino)
        {
            this.apelidoLocal = apelidoLocal;
            this.apelidoDestino = apelidoDestino;
            this.ipDestino = ipDestino;
            this.portaDestino = portaDestino;

            Text = $"Chat Privado - {apelidoLocal} -> {apelidoDestino} ({ipDestino}:{portaDestino})";

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
            try
            {
                cliente.Connect(ipDestino, portaDestino);
                Log($"Conectado ao chat privado {ipDestino}:{portaDestino}");
            }
            catch (Exception ex)
            {
                LogError(ex);
                MessageBox.Show($"Erro ao conectar chat privado: {ex.Message}");
                Close();
                return;
            }
            stream = cliente.GetStream();

            // Envia apelido local ao se conectar
            byte[] apelidoBytes = Encoding.UTF8.GetBytes(apelidoLocal);
            stream.Write(apelidoBytes, 0, apelidoBytes.Length);

            threadReceber = new Thread(() => ReceberMensagens());
            threadReceber.IsBackground = true;
            threadReceber.Start();

            btnEnviar.Click += (s, e) => EnviarMensagem();
        }

        public void AssociarCliente(TcpClient clienteExistente)
        {
            cliente = clienteExistente;
            stream = cliente.GetStream();

            // Lê o apelido de quem se conectou (1a mensagem)
            byte[] buffer = new byte[1024];
            int lidos = stream.Read(buffer, 0, buffer.Length);
            ApelidoRemoto = Encoding.UTF8.GetString(buffer, 0, lidos);

            threadReceber = new Thread(() => ReceberMensagens());
            threadReceber.IsBackground = true;
            threadReceber.Start();
        }

        void EnviarMensagem()
        {
            string msg = txtEntrada.Text.Trim();
            if (msg == "") return;

            string conteudo = $"{apelidoLocal}: {msg}";
            byte[] dados = Encoding.UTF8.GetBytes(conteudo);
            stream.Write(dados, 0, dados.Length);
            txtMensagens.AppendText("Eu: " + msg + Environment.NewLine);
            Log($"Mensagem privada enviada para {apelidoDestino}: {msg}");
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
            catch (Exception ex)
            {
                LogError(ex);
            }
            finally
            {
                cliente?.Close();
            }
        }
    }
}
