using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WebPlatformServer
{
    public class Server
    {
        // solo lo usa esta clase y no cambia
        private readonly IPAddress _address;
        private readonly int _port;
        private readonly string _webRoot;

        // Constructor
        public Server(int port = 8080, string? webRoot = null, IPAddress? address = null)
        {
            _port = port;
            _address = address ?? IPAddress.Loopback;

            // Si no pasan directorio, se usa "static"
            if (string.IsNullOrWhiteSpace(webRoot))
            {
                _webRoot = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "static"));
            }
            else
            {
                _webRoot = Path.GetFullPath(webRoot);
            }
        }

        public void Start()
        {
            var endpoint = new IPEndPoint(_address, _port);
            using var listener = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(endpoint); // Conecta el socket
                listener.Listen(100); // Maximo numero de fila de conexiones
                Console.WriteLine($"[HTTP] Web root: {_webRoot}");
                Console.WriteLine($"[HTTP] Listening on http://{endpoint.Address}:{endpoint.Port}");
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"[HTTP] Bind/Listen failed: {ex.Message}");
                return;
            }

            while (true)
            {
                var client = listener.Accept();

                Task.Run(() =>
                {
                    try
                    {
                        // Pasar a texto los bytes con la funcion getHeaders()
                        string requestText = getHeaders(client);

                        // Depurar con codigo HTTP Parser actividad pasada
                        if (requestText == null)
                        {
                            SendError(client, 400, "Bad Request", "Empty request.");
                            return;
                        }

                        // Si el request esta vacio
                        if (string.IsNullOrWhiteSpace(requestText))
                        {
                            SendError(client, 400, "Bad Request", "Empty request.");
                            return;
                        }

                        var text = requestText.Replace("\r\n", "\n"); //Quitar saltos de linea
                        var lines = text.Split('\n'); // Separar por renglon

                        var requestLine = lines[0].Trim(); // Toma la primera linea y le quita espacios/saltos
                        var parts = requestLine.Split(' ',
                            StringSplitOptions.RemoveEmptyEntries); // Guarda las palabras separadas 

                        if (parts.Length != 3)
                        {
                            SendError(client, 400, "Bad Request", "Empty request.");
                            return;
                        }

                        string method = parts[0].ToUpperInvariant(); // GET
                        string requestTarget = parts[1]; // ej. "/", "/img/logo.png"
                        string protocol = parts[2]; // ej. "HTTP/1.1"

                        if (!protocol.StartsWith("HTTP/", StringComparison.OrdinalIgnoreCase))
                        {
                            SendError(client, 400, "Bad Request", "Invalid HTTP version.");
                            return;
                        }

                        // Solo GET
                        if (method != "GET")
                        {
                            SendResponse(client, 405, "Method Not Allowed", "text/plain; charset=utf-8",
                                "Method Not Allowed", extraHeaders: "Allow: GET\r\n");
                            return;
                        }

                        // Decodificar URL (ej. /Mi%20archivo.html -> /Miarchivo.html)
                        string target = Uri.UnescapeDataString(requestTarget);

                        // "/" -> "/index.html"
                        if (target == "/")
                        {
                            target = "/index.html";
                        }

                        // Sanitizar y construir path seguro
                        if (!TryResolvePath(target, out string? filePath))
                        {
                            SendError(client, 400, "Bad Request", "Invalid path.");
                            return;
                        }

                        // Si no existe, 404
                        if (!File.Exists(filePath))
                        {
                            SendError(client, 404, "Not Found", "Not Found");
                            return;
                        }

                        // Leer bytes y Content-Type
                        byte[] body = File.ReadAllBytes(filePath);
                        string contentType = GetContentType(Path.GetExtension(filePath));

                        SendResponse(client, 200, "OK", contentType, body);
                    }
                    catch (Exception ex)
                    {
                        // Error genérico
                        try
                        {
                            SendError(client, 500, "Internal Server Error", "Internal Server Error");
                        }
                        catch
                        {
                        }

                        Console.WriteLine($"[HTTP] Error: {ex.Message}");
                    }
                    finally
                    {
                        try
                        {
                            client.Shutdown(SocketShutdown.Both);
                        }
                        catch
                        {
                        }

                        client.Dispose();
                    }
                });
            }
        }

        // Leer headers
        private static string getHeaders(Socket client)
        {
            var sb = new StringBuilder(); // Acumulador de texto donde se reconstruye todo lo recibido
            var
                buf = new byte[2048]; // Arreglo temporal de 2048 bytes donde se van guardando los fragmentos que llegan del socket

            // Límite para headers
            const int maxHeaderBytes = 32 * 1024;
            int total = 0; // Contador de bytes leidos en total

            while (true)
            {
                int read = 0;
                try
                {
                    read = client.Receive(buf); // Recibir los bytes
                }
                catch (SocketException)
                {
                    break;
                }

                if (read <= 0) break; // Significa que el cliente cerro la conexion

                total += read;
                if (total > maxHeaderBytes) break;

                sb.Append(Encoding.ASCII.GetString(buf, 0,
                    read)); // Convierte los bytes a texto ASCII y los añade al sb

                // Fin de headers
                if (sb.ToString().Contains("\r\n\r\n")) // Los headers terminan con una línea en blanco (\r\n\r\n).
                    break;
            }

            return sb.ToString();
        }

        // Ej. urlPath = "/index.html"
        private bool TryResolvePath(string urlPath, out string? fullPath)
        {
            fullPath = null;

            // Normalizar separadores
            string relative = urlPath.Replace('/', Path.DirectorySeparatorChar); // -> \\index.html

            // Quitar prefijo de separador si lo hay
            if (relative.StartsWith(Path.DirectorySeparatorChar)) // -> index.html
                relative = relative[1..];

            // No permitimos segmentos peligrosos
            if (relative.Contains("..")) // Detecta que contiene ".." Ej. -> ..\\Windows
                return false;

            // Construir y normalizar ruta absoluta
            string
                combined = Path.GetFullPath(Path.Combine(_webRoot,
                    relative)); // Path.Combine("C:\\servidor\\static", "index.html") -> "C:\\servidor\\static\\index.html"

            // Validar que esté dentro del webRoot
            string root = Path.GetFullPath(_webRoot);
            if (!combined.StartsWith(root,
                    StringComparison.OrdinalIgnoreCase)) // Validar que empieza con "C:\\servidor\\static"
                return false;

            fullPath = combined;
            return true;
        }

        private static void SendError(Socket client, int code, string reason, string message)
            => SendResponse(client, code, reason, "text/plain; charset=utf-8", message);

        private static void SendResponse(Socket client,
            int statusCode,
            string reasonPhrase,
            string contentType,
            string bodyText,
            string extraHeaders = "")
        {
            byte[] body = Encoding.UTF8.GetBytes(bodyText);
            SendResponse(client, statusCode, reasonPhrase, contentType, body, extraHeaders);
        }

        private static void SendResponse(Socket client,
            int statusCode,
            string reasonPhrase,
            string contentType,
            byte[] body,
            string extraHeaders = "")
        {
            string headers =
                $"HTTP/1.1 {statusCode} {reasonPhrase}\r\n" +
                $"Date: {DateTime.UtcNow:R}\r\n" +
                "Server: MinimalCSharpSocketServer/1.0\r\n" +
                $"Content-Type: {contentType}\r\n" +
                $"Content-Length: {body.Length}\r\n" +
                extraHeaders +
                "Connection: close\r\n" +
                "\r\n";

            byte[] headerBytes = Encoding.ASCII.GetBytes(headers);

            client.Send(headerBytes);
            if (body.Length > 0)
                client.Send(body);
        }

        // =============== Tipos MIME ===============

        private static readonly Dictionary<string, string> _mime = new(StringComparer.OrdinalIgnoreCase)
        {
            [".html"] = "text/html; charset=utf-8",
            [".htm"] = "text/html; charset=utf-8",
            [".css"] = "text/css",
            [".js"] = "application/javascript",
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".gif"] = "image/gif",
            [".svg"] = "image/svg+xml",
            [".ico"] = "image/x-icon",
            [".json"] = "application/json; charset=utf-8",
            [".txt"] = "text/plain; charset=utf-8",
        };

        private static string GetContentType(string ext)
        {
            // Si la extension es null, se cambia por cadena vacia para no tener problemas al buscar en el diccionario
            if (ext == null)
            {
                ext = string.Empty;
            }

            // Se busca en el diccionario _mime
            if (_mime.TryGetValue(ext, out string? contentType))
            {
                // Ej. ".html" devuelve "text/html; charset=utf-8"
                return contentType;
            }
            else
            {
                // En ese caso devolvemos un tipo MIME genérico para que igual pueda mostrar el archivo aunque no sepamos su tipo exacto
                return "application/octet-stream";
            }
        }

    }
}

