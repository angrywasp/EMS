# Encrypted Message System

Encrypted Message System (EMS) is a decentralized, encrypted, peer-to-peer messaging system. Each node acts as a store for messages in a collective cloud and to relay connection and message information betwen the network. 

## Encryption

EMS creates a key ring for each user which is a collection of asymmetric encryption keys based on secp256k1 elliptic curve cryptography. The public key forms the public address that other users can use to send you a message. Messages are encrypted/decrypted with a shared key that is created with the senders private key and recipients public key. The shared key is used as a symmetric key for encrypting the message content with AES. Users are able to determine if a message belongs to them by creating their own shared key to decrypt the message.

## Message Format

The message is contructed of the following data

- P2P Header  
    - P2P signature  
    - P2P Protocol version
    - Request byte
        - `Indicates if this message is sent to/from a server`
        - `Always 0x01 to mask the direction of the message`
    - Peer ID
    - Command byte 
        - `Indicates the type of data to expect in the message body`
        - `Byte code 0x0B`
    - Data length

- Message Body
    - Message Hash (32 bytes)
    - Creation timestamp (uint)
    - Hash nonce (uint)
    - Message version (byte)
    - Expiration age in seconds (uint)
    - Encrypted message signature length (ushort)
    - Encrypted message length (ushort)
    - Address-XOR (65 bytes)
    - Read Proof hash (16 bytes, Keccak128)
    - Message signature (Variable length)
    - Encrypted message content (Variable length)
        - Read Proof nonce
        - Message type
        - Message content

## Decryption

As noted above, the message contains an `Address-XOR`. This is the product of the XOR function between the sender and receiver public key. To validate a message a potential recipient must first XOR their public key with this field to obtain the senders public key. This can then be used to verify the message signature. If this message was not intended for you, the XOR function will return a different key than was used to sign the message and the verification process will fail.

If verification is successful, the senders public key (used to verify the signature) is used to created a shared key with the recipients private key. This shared key will be the same as the one used to encrypt the message and should therefore decrypt the message successfully. 

## Message Expiration

Every message has an expiration time, measured in seconds, which starts from the time the message is first constructed. Each nodes deletes the messages from the pool at 5 minute intervals.

## Proof of Work

Proof of work is used as a means of spam prevention and to prevent message pool bloat. Each message must be hashed with a hashing algorithm until a difficulty target is met. The difficulty target is determined by the expiration time. The longer a message is to live in the pool, the more work that must be done to generate the message hash. A minimum life time of 1 hour is also prescribed by the network as a means of deterring low effort spam messages from flooding the network.

## Proof of Receipt (Read Proof)

As well as being able to send messages anonymously, there must be a way for the recipient to be able to signal to the network it has read the message in a manner that  
- Cannot be spoofed by a malicious node  
- Verifies the message was read without revealing who read it  

In order to facilitate this, the sender includes a field in the message body called the `Read Proof Hash`. This is a Proof-of-Work hash result of a randomly selected 128-bit nonce. The nonce is encrypted and stored in the message body. The recipient then verifies they have read the message by providing a message with the hash key of the original message being marked as read and the plain text nonce. Relaying nodes can verify the message by hashing the nonce with the hash function to arrive at the hash result saved in the original message. The message structure is as follows 

- P2P header
    - Same as message header, but with command byte `0x0D`

- Message Body
    - Message key
    - Plain text nonce

This message is quite simple and consists of only the key to look up the matching message in the pool and the plain text nonce which is encrypted in the original message. Who actually sends the message is not of great importance. The nonce is resource intensive to brute force, which serves as a deterrent to try and attack the network by deleting messages from the pool by marking them as read. During verification, this nonce is hashed by the verifying node and compared to the read proof hash included in the matching message. If it matches, we accept it. If a node attempts to send a proof that is invalid, it will be disconnected from the network.


## Privacy Considerations

To prevent tracability, messages will still be propagated after they have been successfully decrypted. This is to prevent tracing the destination of any message. All messages will continue to be propagated to and from all nodes until the message expires, at which time each node removes it from it's own local message pool.

Messages include a Peer ID as part of the P2P protocol header. To Prevent tracing messages from their origin, each node changes the peer ID to their own in the message header before forwarding it. This makes it so at any time in message propagation the Peer ID in the header only shows the last node it was relayed through. This also means that the same message on the network can have multiple different Peer ID values attached to it.

The Address-XOR value is a source of tracability. It exists to obfuscate the recipient address and embed embed a return address for a message and a key to use to verify the message signature. Sending with the same address to the same address will result in this field being the same and allow onlookers to be able to see the number of messages being exchanged between two parties. Therefore outgoing messages should use a new return address to change the value of the `Address-XOR` field on a regular basis. To assist in this, the key ring consists of multiple (default 10) keys, with one randomly selected to sign and encrypt each outgoing message.

## Building from source

This application is only available as a source package. It requires the .NET SDK to build. Download the SDK from [Microsoft](https://dotnet.microsoft.com/download)

`git clone --recursive https://bitbucket.org/angrywasp/encrypted.message.system`  
`cd ./encrypted.message.system`  
`../builder/build <os>`, where `os` is one of
- linux
- win
- osx

## Running the program

`EMS <options>`  

Available options include:  
- `--p2p-port`: Port number to run the P2P network on. Omit for a random port
- `--rpc-port`: Port number to run the RPC interface on. Omit for a random port
- `--rpc-ssl-port`: Port number to run the RPC interface on in SSL mode. Omit for a random port  
- `--key-file`: File path to where your address key is stored. if the file does not exist, it will be created with a new address. Omit to get a temporary address.  
- `--log-file`: Path to save a log file. Warnings and errors are displayed in the console as well as the log file. The log file is only required to capture info messages, which are often save to ignore. Omit to not use a log file.
- `--no-reconnect`: Instructs the node to not attempt a connection to the seed nodes. Will still accept incoming connections. Mostly for development and testing. 
- `--seed-nodes`: A comma delimited list of nodes to use as seed nodes 

*NOTE: SSL support is mostly untested and requires manually installing an SSL certificate on the node server.*

## RPC interface

EMS also includes a JSON RPC API. Please refer to the [RPC](./RPC.md) documentation