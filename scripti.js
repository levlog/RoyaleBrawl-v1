var base = Module.getBaseAddress("Brawl Stars")
Interceptor.attach(base.add(0xE98D8), {
    onEnter: function(args) {
        Interceptor.attach(base.add(0x195FF4), {
            onLeave: function(retval) {
                console.log(retval.toInt32())
            }
        })
    }
})
