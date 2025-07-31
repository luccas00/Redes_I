
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using System;
using System.Net;
using System.Threading;

namespace Chat_TCP;

public class ClienteChat : Form
{
    TextBox txtMensagens, txtEntrada;
    Button btnEnviar;
    TcpClient cliente;
    NetworkStream stream;
    Thread threadReceber;

    public ClienteChat(string apelido, string ipServidor, int porta)
    {
        Text = $"Chat - {apelido}";
        Width = 400;
        Height = 300;

        txtMensagens = new TextBox { Multiline = true, Dock = DockStyle.Top, Height = 200, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
        txtEntrada = new TextBox { Dock = DockStyle.Fill };
        btnEnviar = new Button { Text = "Enviar", Dock = DockStyle.Right, Width = 75 };

        var painelInferior = new Panel { Dock = DockStyle.Bottom, Height = 30 };
        painelInferior.Controls.Add(txtEntrada);
        painelInferior.Controls.Add(btnEnviar);

        Controls.Add(txtMensagens);
        Controls.Add(painelInferior);

        cliente = new TcpClient();
        cliente.Connect(ipServidor, porta);
        stream = cliente.GetStream();

        threadReceber = new Thread(() => ReceberMensagens());
        threadReceber.Start();

        btnEnviar.Click += (s, e) => EnviarMensagem(apelido);
    }

    void EnviarMensagem(string apelido)
    {
        string mensagem = txtEntrada.Text.Trim();
        if (mensagem == "") return;

        string conteudo = $"{apelido}: {mensagem}";
        byte[] buffer = Encoding.UTF8.GetBytes(conteudo);
        stream.Write(buffer, 0, buffer.Length);
        txtEntrada.Clear();
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
                Invoke((MethodInvoker)(() => txtMensagens.AppendText(msg + Environment.NewLine)));
            }
        }
        catch { }
        finally
        {
            cliente.Close();
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