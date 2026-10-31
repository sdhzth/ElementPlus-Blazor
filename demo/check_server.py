import http.client

for p in ['/', '/data']:
    try:
        c = http.client.HTTPConnection('127.0.0.1', 5199, timeout=5)
        c.request('GET', p)
        r = c.getresponse()
        body = r.read(300)
        print(p, r.status, r.getheader('Content-Type'))
        c.close()
    except Exception as e:
        print(p, 'ERR', repr(e))
