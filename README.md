Ftps Client
===================
This is simple .NET client for implicit's FTPS protocol.

##Usage Example

```
//Create client instance.
FtpsClient client = new FtpsClient();

//Specify remote host configurations and connect to it.
client.Connect(new FtpsClientConfigurations() {
	Host = "your-host-ip",
	Port = 990,
	Username = "your-user",
	Password = "your-password",
	SocketReadTimeout = 1000,
	SocketWriteTimeout = FtpsClient.INFINITE_TIMEOUT,
});

//Navigate to remote directory.
client.NavigateTo("/");

//Upload a file.
client.UploadFile("my-local-file-path", "my-remote-file-path");

//Download a file
client.DownloadFile("my-remote-file-path", "my-local-file-path");

//Disconnect
client.Disconnect();
```

## Author
[Albert Lloveras Carbonell](https://github.com/alloveras)

## License
Copyright (c) 2016 V2MSoftware
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.