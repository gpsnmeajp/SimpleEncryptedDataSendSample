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
    HttpListener listener;
    Thread thread = null;
    public string adr = "http://127.0.0.1:8000/";
    public string password = "1234";
    public byte[] data;

    readonly UTF8Encoding utf8 = new UTF8Encoding(false);

    SynchronizationContext MainThreadContext;
    Action<byte[]> OnReceived = null;

    // Start is called before the first frame update
    void Start()
    {
        MainThreadContext = SynchronizationContext.Current;

        data = utf8.GetBytes("Welcome");

        listener = new HttpListener();
        listener.Prefixes.Add(adr);
        listener.Start();

        //受信処理スレッド
        thread = new Thread(new ThreadStart(ReceiveThread));
        thread.Start();
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnDestroy()
    {
        listener.Close();
        thread.Join();
    }

    private void ReceiveThread()
    {
        try
        {
            while (listener.IsListening)
            {
                HttpListenerContext context = listener.GetContext();
                HttpListenerRequest request = context.Request;

                //Console.WriteLine(request.Url.LocalPath);
                HttpListenerResponse response = context.Response;

                response.StatusCode = 400;
                string res = ("400 Bad Request");
                try
                {
                    if (request.HttpMethod == "POST" && request.HasEntityBody)
                    {
                        switch (request.Url.LocalPath)
                        {
                            case "/request":
                                {
                                    var len = request.ContentLength64;
                                    byte[] rcvBuf = new byte[len];
                                    request.InputStream.Read(rcvBuf, 0, (int)len);
                                    string str = utf8.GetString(rcvBuf);
                                    str = System.Web.HttpUtility.UrlDecode(str);

                                    res = RijndaelEncryption.Decrypt(str, password);
                                    Debug.Log(res);
                                    if (res == "request")
                                    {
                                        response.StatusCode = 200;
                                        res = Convert.ToBase64String(RijndaelEncryption.Encrypt(data, password));
                                    }
                                }
                                break;
                            case "/upload":
                                {
                                    var len = request.ContentLength64;
                                    byte[] rcvBuf = new byte[len];
                                    request.InputStream.Read(rcvBuf, 0, (int)len);
                                    string str = utf8.GetString(rcvBuf);
                                    str = System.Web.HttpUtility.UrlDecode(str);

                                    Debug.Log(str);

                                    res = RijndaelEncryption.Decrypt(str, password);
                                    Debug.Log(res);

                                    response.StatusCode = 200;
                                    res = RijndaelEncryption.Encrypt("Upload OK", password);
                                }
                                break;
                            default:
                                res = ("404 Not found");
                                response.StatusCode = 404;
                                break;
                        }
                    }
                }
                catch (Exception e)
                {
                    response.StatusCode = 500;
                    res = ("Internal Server Error");
                    Debug.Log(e);
                }

                byte[] buf = utf8.GetBytes(res);
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

