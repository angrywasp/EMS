# Encrypted Message System RPC Interface

EMS contains a basic RPC interface for performing common tasks and integrating EMS into other services and frontends.

All RPC functions return a consistent JSON formatted response, consisting of 3 root fields, `response`,  `status` and `code`.  

``` json
{ 
   "response":{ 
      ...
   },
   "status":"OK",
   "code":0
}
```

The status field is either `OK` or `ERROR` depending on success or failure of the RPC request. In addition the RPC interface returns a http status code of 200 for success and 400 for failure. This provides 2 simple methods of checking if an RPC function has been successful. If returning http code 400 or JSON status `ERROR` the response should be discarded. Additionally, the `code` field returns a function specific RPC error code, or 0 in the case of no error.

## RPC functions

Below is a list of the available RPC functions and an example of making the RPC request in bash and expected response. Examples bash scripts are also contained in `./rpc-test`.  

### **get_address**

Returns the address currently in use by the node. [Bash example](./rpc-test/get_address)

**Response**
``` json
{ 
    "response":{ 
        "public":"NqeVhcQPYH8ttKcygShHcgtvTQXgqayk8bVudkFRJaVYezdSzaktyQWavw59F4ehU2gCZUJPUDoYuFYKDpSD16VH",
        "public_hex":"04444ecddeddce4b9640ad1b73219376852efcccdad9f9428c5f5a913affb1e29fced7f829e93f5c361e07b1eda31326215cf62df06dce6c392f6917ce00e7053c",
        "private":"jNDGXhGL5DH31UHMwRW9qVUssyCLSyfbhSj7QgDm7h",
        "private_hex":"2fe72f6e9078ab6d80b9af90e098718d1997a361f6fd6956ccb598ec41ebe4"
    },
    "status":"OK"
}
```

**Input:**  

- `private`: Should the response include the private keys?

**Output:**  

- `public`: Base58 encoded public key. This is the address people use to send you messages.  
- `public_hex`: Hex representation of the public key.  
- `private`: Base58 encoded private key. This is required to restore your address.  
- `private_hex`: Hex representation of the private key.  

**Notes** 

If `private` is set to false in the request, both `private` and `private_hex` response fields will be 0 length strings

**Error Codes**

- `100`: The node is configured with `--relay-only` and the node address is not available.

### **get_message_count**

Returns a count of the messages in the pool. [Bash example](./rpc-test/get_message_count)

**Response**
``` json
{ 
    "response":{ 
        "total":1,
        "decrypted":1
    },
    "status":"OK"
}
```

**Input:**  

- None

**Output:**    

- `total`: The number of messages in the message pool.  
- `decrypted`: The number of decrypteds messages. i.e. The number of messages you have sent or received.  

### **get_message_details**

Gets basic details of messages in the pool

**Response**  
``` json
{
  "response": {
    "details": [
      {
        "key": "c2679f2f708bb8fa44d3cb9a7463b8ae",
        "timestamp": 1581252892,
        "expiration": 3600,
        "address": "PyLmzy5yzP1QNFvFanDpVRgdXcQv7tK6zoVJPrdFUTvErbTbjGAEbirMPhUHZNTxEj4FLaLojycccMfMiixEaeYR",
        "read": true,
        "direction": "in"
      },
      {
        "key": "a6c2372887b9ea6eb322e1d4d8cd252c",
        "timestamp": 1581252855,
        "expiration": 3600,
        "address": "RVuxUERcDAKqiSQGX2DMR7LKLDSpZGVwZrX5zeKuvupwkwkMPbQf63igVQLTfhqeZur3rGWpN2JfuANizyForYXN",
        "read": false,
        "direction": "out"
      },
      {
        "key": "bbafb333a94e8faca9a714750c453c5a",
        "timestamp": 1581252883,
        "expiration": 3600,
        "address": "",
        "read": false,
        "direction": ""
      }
    ]
  },
  "status": "OK",
  "code": 0
}
```

**Input:**  

- None

**Output:**  

- `details`: An array of messages in the pool.   
    - `key`: The key of the message.  
    - `timestamp`: The creation timestamp of the message.  
    - `expiration`: The lifetime of the message in seconds.  
    - `address`: The address of the message. Empty if the message is encrypted.  
    - `read`: Has this message previously been read?
    - `direction`: The status of the message, whether incoming or outgoing. Empty if the message is encrypted.

**Notes**

The address field indicates different things depending on the status of direction  
If `direction = "in"`, the address is the address of the party sending the message  
If `direction = "out"`, the address is is the address of the party receiving the message

### **get_message**

Retrieves a message to read. [Bash example](./rpc-test/get_message)

**Response**  
``` json
{
  "response": {
    "key": "4916fcc2c58305e7850e7bc0bc636fe5",
    "hash": "0811222165df66f8781606bf6aba6a718c1d6d4c748956ce9546f47a8b020000",
    "timestamp": 1581253880,
    "expiration": 3600,
    "address": "RVuxUERcDAKqiSQGX2DMR7LKLDSpZGVwZrX5zeKuvupwkwkMPbQf63igVQLTfhqeZur3rGWpN2JfuANizyForYXN",
    "message": "dGhpcyBpcyBhbm90aGVyIHRlc3QgbWVzc2FnZQ==",
    "read_proof": {
      "nonce": "00000000000000000000000000000000",
      "hash": "0000000000000000000000000000000000000000000000000000000000000000",
      "read": false
    }
  },
  "status": "OK",
  "code": 0
}
```

**Input:**  

- `key`: The hash key of the message you want to read. Obtained with the `get_message_details` RPC endpoint

**Output:**  

- `incoming`: Flag indicating if this is an incoming or outgoing message.  
- `timestamp`: The Unix timestamp of the time the message was created.  
- `destination`: The address that authored the message. 
- `message`: The text of the message

**Notes**

The [get_message](./rpc-test/get_message) function returns the message as a base64 encoded string. The test script demonstrates the process.  

The output of the `address` field depends on the value of direction.  
If `direction = "in"`, the address is the address of the party sending the message.  
If `direction = "out"`, the address is is the address of the party receiving the message.

The output of the `read_proof` field depends on the value of direction.  
If `direction = "in"`, the read proof will be populated with valid values.  
If `direction = "out"`, the read proof will zero strings of the correct length.  
In both cases, the value of `read` can be used to determine if a message is actually read.

### **send_message**

Send a message to another user. [Bash example](./rpc-test/send_message)

**Response**  
``` json
{
    "response": {
        "result": "36774ad4af5018e7931419af91ad0bf5"
    },
    "status": "OK",
    "code": 0
}
```

**Input:**  

- `address`: The address to send the message to.  
- `message`: The message to send.

**Output:**  

- `key`: The hash key of the newly created message.  

**Notes**

The [send_message](./rpc-test/send_message) function expects the message data a base64 encoded string. The test script demonstrates the process.  

This function can take some time to return. After sending the message, it must be hashed with the PoW hashing algorithm on the server side.  
The function will return in due time when the process is complete.

**Error Codes**

- `100`: The node is configured with `--relay-only` and the node address is not available.  
- `200`: Sending of the message failed for any other reason.