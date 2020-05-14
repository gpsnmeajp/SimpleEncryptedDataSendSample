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
    public int port = 8000;

    string password = "";
    byte[] data;

    public Action<byte[]> OnReceived = null;

    readonly UTF8Encoding utf8 = new UTF8Encoding(false);
    HttpListener listener;
    Thread thread = null;
    SynchronizationContext MainThreadContext;

    public void SetData(byte[] data)
    {
        this.data = data;
    }
    public void SetPassword(string password)
    {
        this.password = password;
    }

    void Start()
    {
        MainThreadContext = SynchronizationContext.Current;

        listener = new HttpListener();
        listener.Prefixes.Add("http://*:"+port+"/");
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
                        switch (request.Url.LocalPath)
                        {
                            case "/request":
                                {
                                    var len = request.ContentLength64;
                                    byte[] rcvBuf = new byte[len];
                                    request.InputStream.Read(rcvBuf, 0, (int)len);

                                    byte[] decryptedReceiveData = RijndaelEncryption.Decrypt(rcvBuf, password);

                                    if (utf8.GetString(decryptedReceiveData) == "request")
                                    {
                                        response.StatusCode = 200;
                                        res = RijndaelEncryption.Encrypt(data, password);
                                    }
                                }
                                break;
                            case "/upload":
                                {
                                    var len = request.ContentLength64;
                                    byte[] rcvBuf = new byte[len];
                                    request.InputStream.Read(rcvBuf, 0, (int)len);

                                    byte[] decryptedReceiveData = RijndaelEncryption.Decrypt(rcvBuf, password);

                                    OnReceived?.Invoke(decryptedReceiveData);

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
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }
}

