# vapix-api-regression-tests

Automated API regression testing for network cameras (work in progress).

`src/MockCamera` is a small ASP.NET Core minimal API that imitates the shape of the public
VAPIX endpoint `/axis-cgi/basicdeviceinfo.cgi`. All data is fake. Not affiliated with Axis Communications.

```bash
dotnet run --project src/MockCamera
```
