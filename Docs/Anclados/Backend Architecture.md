# Unity Ship Game Backend Architecture

- Client
- Server

# Server/Client Abstraction

## Things to abstract

- Server Creation encapsulated in a class
    - holds the socket, the ip, the connected peers
    - Functions to send and recieve data from the users
    - Emit events when player connected, disconected, server created, server destroyed, etc...
    

- Client Creation encapsulated in a class
    - Connected Server,
    - Functions to send data to the server and recieve it
    - Emit events when connected to server, disconnected from the server, etc...

- Some class that abstracts common things
    - Abstracts Socket creation, udp/tcp, ip, port, etc...
    - Abstracts data serialization