package Redes1.API_Chat_TCP;

import jakarta.annotation.PostConstruct;
import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.RestController;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.InetSocketAddress;
import java.net.Socket;
import java.nio.charset.StandardCharsets;
import java.util.List;
import java.util.concurrent.CopyOnWriteArrayList;

@RestController
public class Controller {

    @PostMapping("/enviar")
    public ResponseEntity<String> enviarMensagemBroadcast(@RequestBody ChatDTO dto) {
        String apelido = "API_Chat_TCP";
        int portaPrivada = 1998; // Valor arbitrário fixo
        String dadosConexao = apelido + ";" + portaPrivada;

        try (Socket socket = new Socket("127.0.0.1", 1998)) {
            OutputStream out = socket.getOutputStream();

            // 1. Enviar conexão inicial
            out.write(dadosConexao.getBytes(StandardCharsets.UTF_8));
            out.flush();
            Thread.sleep(100); // Dá tempo do servidor processar o registro

            // 2. Enviar mensagem real como broadcast
            String mensagem = "[Broadcast] " + apelido + ": " + dto.mensagem();
            out.write(mensagem.getBytes(StandardCharsets.UTF_8));
            out.flush();

            return ResponseEntity.ok("Mensagem enviada com sucesso.");
        } catch (IOException | InterruptedException e) {
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                    .body("Erro ao enviar mensagem: " + e.getMessage());
        }
    }

    @GetMapping("/enviar")
    public ResponseEntity<String> enviarMensagemBroadcastViaUrl(@RequestParam String mensagem) {
        String apelido = "API_Chat_TCP";
        int portaPrivada = 1998; // Valor arbitrário fixo
        String dadosConexao = apelido + ";" + portaPrivada;

        try (Socket socket = new Socket("127.0.0.1", 1998)) {
            OutputStream out = socket.getOutputStream();

            // 1. Enviar conexão inicial
            out.write(dadosConexao.getBytes(StandardCharsets.UTF_8));
            out.flush();
            Thread.sleep(100); // Dá tempo do servidor processar o registro

            // 2. Enviar mensagem real como broadcast
            String conteudo = "[Broadcast] " + apelido + ": " + mensagem;
            out.write(conteudo.getBytes(StandardCharsets.UTF_8));
            out.flush();

            Thread.sleep(1000);
            enviarPing();
            Thread.sleep(500);
            enviarPong();

            return ResponseEntity.ok("Mensagem enviada com sucesso.");

        } catch (IOException | InterruptedException e) {
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                    .body("Erro ao enviar mensagem: " + e.getMessage());
        }
    }

    public ResponseEntity<String> enviarPing() {
        String apelido = "API_Chat_TCP";
        int portaPrivada = 1998; // Valor arbitrário fixo
        String dadosConexao = apelido + ";" + portaPrivada;

        try (Socket socket = new Socket("127.0.0.1", 1998)) {
            OutputStream out = socket.getOutputStream();

            // 1. Enviar conexão inicial
            out.write(dadosConexao.getBytes(StandardCharsets.UTF_8));
            out.flush();
            Thread.sleep(100); // Dá tempo do servidor processar o registro

            // 2. Enviar mensagem real como broadcast
            String conteudo = "[Broadcast] " + apelido + ": Ping";
            out.write(conteudo.getBytes(StandardCharsets.UTF_8));
            out.flush();

            return ResponseEntity.ok("Mensagem enviada com sucesso.");

        } catch (IOException | InterruptedException e) {
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                    .body("Erro ao enviar mensagem: " + e.getMessage());
        }
    }

    public ResponseEntity<String> enviarPong() {
        String apelido = "API_Chat_TCP";
        int portaPrivada = 1998; // Valor arbitrário fixo
        String dadosConexao = apelido + ";" + portaPrivada;

        try (Socket socket = new Socket("127.0.0.1", 1998)) {
            OutputStream out = socket.getOutputStream();

            // 1. Enviar conexão inicial
            out.write(dadosConexao.getBytes(StandardCharsets.UTF_8));
            out.flush();
            Thread.sleep(100); // Dá tempo do servidor processar o registro

            // 2. Enviar mensagem real como broadcast
            String conteudo = "[Broadcast] " + apelido + ": Pong";
            out.write(conteudo.getBytes(StandardCharsets.UTF_8));
            out.flush();

            return ResponseEntity.ok("Mensagem enviada com sucesso.");

        } catch (IOException | InterruptedException e) {
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                    .body("Erro ao enviar mensagem: " + e.getMessage());
        }
    }

    @GetMapping("/ping")
    public ResponseEntity<String> pingServidor() {
        try (Socket socket = new Socket()) {
            socket.connect(new InetSocketAddress("127.0.0.1", 1998), 2000);
            if (socket.isConnected()) {
                return ResponseEntity.ok("Servidor online e aceitando conexões.");
            } else {
                return ResponseEntity.status(HttpStatus.SERVICE_UNAVAILABLE).body("Servidor offline.");
            }
        } catch (IOException e) {
            return ResponseEntity.status(HttpStatus.SERVICE_UNAVAILABLE)
                    .body("Erro ao conectar no servidor: " + e.getMessage());
        }
    }

    @GetMapping("/usuarios/listar")
    public ResponseEntity<String> listarUsuarios() {
        try (Socket socket = new Socket("127.0.0.1", 1998)) {
            OutputStream out = socket.getOutputStream();
            InputStream in = socket.getInputStream();

            // Envia comando para listar usuários
            out.write("/lista".getBytes(StandardCharsets.UTF_8));
            out.flush();

            byte[] buffer = new byte[4096];
            int lidos = in.read(buffer);

            String resposta = new String(buffer, 0, lidos, StandardCharsets.UTF_8);
            return ResponseEntity.ok(resposta);
        } catch (IOException e) {
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                    .body("Erro ao listar usuários: " + e.getMessage());
        }
    }

    @GetMapping("/usuarios/count")
    public ResponseEntity<String> contarUsuarios() {
        try (Socket socket = new Socket("127.0.0.1", 1998)) {
            OutputStream out = socket.getOutputStream();
            InputStream in = socket.getInputStream();

            // Envia comando para contar usuários
            out.write("/count".getBytes(StandardCharsets.UTF_8));
            out.flush();

            byte[] buffer = new byte[1024];
            int lidos = in.read(buffer);

            String resposta = new String(buffer, 0, lidos, StandardCharsets.UTF_8);
            return ResponseEntity.ok(resposta);
        } catch (IOException e) {
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                    .body("Erro ao contar usuários: " + e.getMessage());
        }
    }



}

