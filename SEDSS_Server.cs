/*
MIT License

Copyright (c) 2020 gpsnmeajp

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using System;
using System.Threading;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityCipher;


public class SEDSS_Server : MonoBehaviour
{
    public string domain = "*";
    public int port = 8000;

    string password = "";

    public Action<byte[], string> OnDataUploaded = null;
    public Func<string, byte[]> OnDownloadRequest = null;

    readonly UTF8Encoding utf8 = new UTF8Encoding(false);
    HttpListener listener;
    Thread thread = null;
    SynchronizationContext MainThreadContext;

    public void SetPassword(string password)
    {
        this.password = password;
    }

    void Start()
    {
        MainThreadContext = SynchronizationContext.Current;

        listener = new HttpListener();
        listener.Prefixes.Add("http://" + domain + ":" + port + "/");
        listener.Start();

        //受信処理スレッド
        thread = new Thread(new ThreadStart(ReceiveThread));
        thread.Start();
    }

    private void OnDestroy()
    {
        try
        {
            listener.Close();
        }
        catch (Exception e)
        {
            //Do noting
        }
        finally
        {
            thread.Join();
        }
    }

    private void ReceiveThread()
    {
        try
        {
            while (listener.IsListening)
            {
                HttpListenerContext context = listener.GetContext();
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                response.StatusCode = 400;
                byte[] res = utf8.GetBytes("400 Bad Request");
                try
                {
                    if (request.HttpMethod == "PUT" && request.HasEntityBody)
                    {
                        string id = RijndaelEncryption.Decrypt(request.Url.Query.Remove(0, 1), password);

                        //Debug.Log(id);

                        var len = request.ContentLength64;
                        //Debug.Log(len);
                        byte[] rcvBuf;

                        byte[] membuf = new byte[256];
                        using (var memoryStream = new MemoryStream())
                        {
                            while (true)
                            {
                                int readlen = request.InputStream.Read(membuf, 0, membuf.Length);
                                if (readlen <= 0)
                                {
                                    break;
                                }
                                memoryStream.Write(membuf, 0, readlen);
                            }
                            rcvBuf = memoryStream.ToArray();
                        }
                        //Debug.Log(rcvBuf.Length);

                        byte[] decryptedReceiveData;
                        try
                        {
                            decryptedReceiveData = RijndaelEncryption.Decrypt(rcvBuf, password);
                        }
                        catch (Exception)
                        {
                            throw new ArgumentException("Decryption Error");
                        }
                        switch (request.Url.LocalPath)
                        {
                            case "/request":
                                {
                                    if (utf8.GetString(decryptedReceiveData) == "request")
                                    {
                                        response.StatusCode = 200;
                                        byte[] data = OnDownloadRequest?.Invoke(id);
                                        res = RijndaelEncryption.Encrypt(data, password);
                                    }
                                }
                                break;
                            case "/upload":
                                {
                                    MainThreadContext.Post((state) => {
                                        OnDataUploaded?.Invoke(decryptedReceiveData, id);
                                    }, null);

                                    response.StatusCode = 200;
                                    string responseString = ("Upload OK");
                                    res = RijndaelEncryption.Encrypt(utf8.GetBytes(responseString), password);
                                }
                                break;
                            default:
                                res = utf8.GetBytes("404 Not found");
                                response.StatusCode = 404;
                                break;
                        }
                    }
                }
                catch (Exception e)
                {
                    response.StatusCode = 400;
                    res = utf8.GetBytes("400 Bad Request");
                    Debug.Log(e);
                }

                Byte[] buf = res;
                response.OutputStream.Write(buf, 0, buf.Length);
                response.OutputStream.Close();

                Thread.Sleep(30);
            }
        }
        catch (HttpListenerException)
        {
            //Do noting
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }
}

