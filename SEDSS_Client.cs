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
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityCipher;

public class SEDSS_Client : MonoBehaviour
{
    public int Port = 8000;

    string Address = "";
    string password = "";
    readonly UTF8Encoding utf8 = new UTF8Encoding(false);

    public void SetAddress(string Address) {
        this.Address = Address;
    }

    public void SetPassword(string password)
    {
        this.password = password;
    }

    //データを暗号化してアップロードする。
    //成功すればOnOnSuccess、エラー発生時にはOnErrorを叩く。
    public void Upload(byte[] data,string id, Action<string> OnSuccess, Action<string,string> OnError)
    {
        StartCoroutine(UploadCoroutine(data, id, OnSuccess, OnError));
    }

    //暗号化されたデータをダウンロードするする。
    //成功すればOnOnSuccess、エラー発生時にはOnErrorを叩く。
    public void Download(string id,Action<byte[],string> OnSuccess, Action<string, string> OnError)
    {
        StartCoroutine(RequestCoroutine(id,OnSuccess, OnError));
    }

    IEnumerator UploadCoroutine(byte[] data, string id, Action<string> OnSuccess, Action<string,string> OnError)
    {
        if (id == null || id == "") {
            id = "-";
        }
        if (data == null || data.Length == 0) {
            OnError?.Invoke("No data", id);
            yield break;
        }

        UnityWebRequest req;
        try
        {
            string URL = "http://" + Address + ":" + Port + "";
            byte[] encryptedData = RijndaelEncryption.Encrypt(data, password);
            string encryptedId = RijndaelEncryption.Encrypt(id, password);
            req = UnityWebRequest.Put(URL + "/upload?"+encryptedId, encryptedData);
        }
        catch (Exception e)
        {
            OnError?.Invoke(e.ToString(),id);
            yield break;
        }

        yield return req.SendWebRequest();

        try
        {
            if (req.isNetworkError || req.isHttpError)
            {
                OnError?.Invoke(req.error, id);
                yield break;
            }
            if (req.responseCode != 200)
            {
                OnError?.Invoke("CODE:" + req.responseCode, id);
                yield break;
            }
            if (req.responseCode != 200)
            {
                OnError?.Invoke("CODE:" + req.responseCode, id);
                yield break;
            }

            byte[] response;
            try
            {
                response = RijndaelEncryption.Decrypt(req.downloadHandler.data, password);
            }
            catch (Exception) {
                throw new ArgumentException("Decryption Error");
            }
            if (utf8.GetString(response) != "Upload OK") {
                OnError?.Invoke("DecrtptFail", id);
                yield break;
            }

            OnSuccess?.Invoke(id);
            yield break;
        }
        catch (Exception e) {
            OnError?.Invoke(e.ToString(), id);
            yield break;
        }
    }

    IEnumerator RequestCoroutine(string id, Action<byte[],string> OnSuccess, Action<string, string> OnError)
    {
        if (id == null || id == "")
        {
            id = "-";
        }

        UnityWebRequest req;
        try
        {
            string URL = "http://" + Address + ":" + Port + "";

            string requestData = "request";
            byte[] keywordBytes = utf8.GetBytes(requestData);
            byte[] encryptedData = RijndaelEncryption.Encrypt(keywordBytes, password);
            string encryptedId = RijndaelEncryption.Encrypt(id, password);
            req = UnityWebRequest.Put(URL + "/request?"+ encryptedId, encryptedData);
        }
        catch (Exception e)
        {
            OnError?.Invoke(e.ToString(),id);
            yield break;
        }

        yield return req.SendWebRequest();

        try
        {
            if (req.isNetworkError || req.isHttpError)
            {
                OnError?.Invoke(req.error,id);
                yield break;
            }
            if (req.responseCode != 200)
            {
                OnError?.Invoke("CODE:" + req.responseCode,id);
                yield break;
            }
            byte[] data;
            try
            {
                data = RijndaelEncryption.Decrypt(req.downloadHandler.data, password);
            }
            catch (Exception)
            {
                throw new ArgumentException("Decryption Error");
            }
            OnSuccess?.Invoke(data, id);
            yield break;
        }
        catch (Exception e)
        {
            OnError?.Invoke(e.ToString(), id);
            yield break;
        }
    }
}
