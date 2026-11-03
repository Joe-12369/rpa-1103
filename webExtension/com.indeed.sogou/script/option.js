var port = chrome.extension.connect()

document.addEventListener('click', function (e) {
    console.log('click', e.target.id)
    switch (e.target.id) {
        case 'connect':
            {
                connect_server();
                break
            }      
        default: break;
    }
})

