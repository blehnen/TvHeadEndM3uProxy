# TvHeadEndM3uProxy

This is a proxy that will download and modify the channels list returned by TvHeadend (https://tvheadend.org/)

The channel list for tvheadend contains tickets that expire after 300 seconds; This causes problems if you are using xteve (https://xteve.de/) to server your channels to Plex.

This proxy will modify the channels in the m3u list so that they are in this format instead

Original

http://127.0.0.1:9981/stream/channelid/1234?ticket=dfdkdjflsdjfdsl&profile=pass

Modified

http://username:password@127.0.0.1:9981/stream/channelid/1234&profile=pass

This will allow plex (or Xteve) to connect to TvHeadend with authentication.

Installation
-------------

* TvHeadEndM3uProxy.exe install

This will install the proxy as a windows service.  The default port it will listen on is 33721

Configuration
-------------

Settings are located in TvHeadEndM3uProxy.json

You'll need to provide the following


| Setting	 | Value	 | Example |
| --- | --- | --- |
| TvHeadendAddress	| Location of Tvheadend	| http://127.0.0.1:9981
| TvHeadEndUserName	| Username				| tvuser
| TvHeadEndPassword | Password				| apassword

You can connect and download the modified file via a browser -

http://youripaddress:33721/api/tvheadend/channels



