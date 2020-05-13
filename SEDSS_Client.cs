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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityCipher;

public class SEDSS_Client : MonoBehaviour
{
    public string URL = "http://127.0.0.1:8000";
    public string password = "1234";

    // Start is called before the first frame update
    void Start()
    {
        //StartCoroutine(Request());
        StartCoroutine(Upload());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator Request()
    {
        UnityWebRequest req = UnityWebRequest.Post(URL + "/request", RijndaelEncryption.Encrypt("request", password));
        yield return req.SendWebRequest();

        if (req.isNetworkError || req.isHttpError)
        {
            Debug.Log(req.error);
        }
        else
        {
            Debug.Log(RijndaelEncryption.Decrypt(req.downloadHandler.text, password));
        }
    }
    IEnumerator Upload()
    {
        UnityWebRequest req = UnityWebRequest.Post(URL + "/upload", RijndaelEncryption.Encrypt("my data", password));
        yield return req.SendWebRequest();

        if (req.isNetworkError || req.isHttpError)
        {
            Debug.Log(req.error);
        }
        else
        {
            Debug.Log(RijndaelEncryption.Decrypt(req.downloadHandler.text, password));
        }
    }
}
