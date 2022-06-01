# ObST - OpenAPI-based Stateful Testing

ObST is a commandline testing tool to automatically validate RESTful Webservice using it's OpenAPI specification.

## Usage

1. Generate the test configuration using the OpenAPI specification.

   ```
   ObST analyze --in oas.yaml
   ```

2. Check the generated output (required for complex APIs).

3. Run tests using the generated test configuration.

   ```
   ObST test
   ```

## Test Configuration

The generated test configuration is made up of the following sections:

1. **Setup**

   Contains the generic test configurations

   ```yaml
   resetUri: PLEASE SET MANUALLY
     quickCheck:
       doNotShrink: false
       maxNbOfTest: 100
       startSize: 2
       endSize: 5
     generator:
       ignoreOptionalPropertiesFrequency: 10
       nullValueForNullableFrequency: 10
       useKnownIdFrequency: 95
       useInvalidOrNullIdentityFrequency: 5
     properties:
       responseDocumentation: false
       noBadRequestWhenValidDataIsProvided: false
   ```

2. **Servers**

   Configures the servers of the SUT

   ```yaml
   - &o1
     url: http://localhost:5000
     description: Fallback added form Swagger url
   ```

3. **Resource Classes**

   Defines the different resource classes of the SUT

   ```yaml
   Author: &o0
     id: :@id
     name: :name
   TodoItem: &o7
     id: :@id
     name: :name
     isComplete: :isComplete
   ```
 
4. **Pathes**

   Defines the pathes of the SUT as a tree.
   Variables are encoded with curly brackets.

   ```yaml
   api:
     Author:
       $type: Author[]
       '{Author:@id}':
         $type: Author
     Todo:
       $type: TodoItem[]
       '{TodoItem:@id}':
         $type: TodoItem
   ```
  
5. **Parameters** (optional)

   Defines the parameters (query|header|cookie) used by the SUT.

   ```yaml
   Get /api/Todo:
     query:
       limit: UNKNOWN_0
   ```

6. **Security Schemes** (optional)

   Defines the used security schemes.

   ```yaml
   type: apiKey
   name: secret
   in: header
   ```

7. **Operations**

   Defines the operations of the SUT in an OpenAPI-like style with added information.

   ```yaml
    /api/Author:
      Get:
        operationId: Get /api/Author
        parameters: {}
        responses:
          200:
            content:
              application/json:
                schema:
                  resourceClass: *o0
                  type: array
                  items: &o2
                    resourceClass: *o0
                    type: object
                    required:
                    - name
                    properties:
                      id:
                        type: integer
                        format: int64
                      name:
                        type: string
                    additionalPropertiesAllowed: false
        servers:
        - *o1
   ```

## Reading the test result log

The test tool displays at the end of each run coverage statistics showing all the covered, documented but not found and found but not documented status codes for each operation.

```
[15:03:12 INF] Get /api/Author Covered: 200 - Not covered:  - Not documented:
[15:03:12 INF] Post /api/Author Covered: 201, 400 - Not covered:  - Not documented:
[15:03:12 INF] Get /api/Author/{?} Covered: 200, 404 - Not covered:  - Not documented:
[15:03:12 INF] Put /api/Author/{?} Covered: 404 - Not covered:  - Not documented: 400, 204
[15:03:12 INF] Delete /api/Author/{?} Covered: 404 - Not covered:  - Not documented: 204
[15:03:12 INF] Get /api/Todo Covered: 200 - Not covered:  - Not documented:
[15:03:12 INF] Post /api/Todo Covered: 201, 400 - Not covered:  - Not documented:
[15:03:12 INF] GetTodo Covered: 200, 404 - Not covered:  - Not documented:
[15:03:12 INF] Put /api/Todo/{?} Covered: 404 - Not covered:  - Not documented: 204, 400
[15:03:12 INF] Delete /api/Todo/{?} Covered: 404 - Not covered:  - Not documented: 204
[15:03:12 INF] Get /api/Todo/{?}/comments Covered: 200 - Not covered:  - Not documented: 404
[15:03:12 INF] Post /api/Todo/{?}/comments Covered: 201 - Not covered:  - Not documented: 400, 404
[15:03:12 INF] Get /api/Todo/{?}/comments/{?} Covered: 200, 404 - Not covered:  - Not documented:
[15:03:12 INF] Delete /api/Todo/{?}/comments/{?} Covered: 404 - Not covered:  - Not documented:
```

Once the test tool discoveres a violation of one of the enabled properties the operations which lead to the violations are logged.
Additionally a shrunk sequence of operations is logged which cause the same violations with a shorter sequence length (local minimum).

```
[E] Get /api/Todo/{?}/comments: {"Query":{},"Header":{},"Path":["1133866475"],"Cookie":{},"Body":null,"UsedGeneratorOptions":{"TodoItem:@id":{"Item1":2,"Item2":"1133866475"}},"Identity":{"Id":"NULL_IDENTITY","SecuritySchemeName":"NULL_IDENTITY","SecurityScheme":{"Type":0,"Name":null,"In":null,"OpenIdConnectUrl":null},"ApiKey":null,"Scopes":[],"ClientId":null,"ClientSecret":null},"IdentityHasAllPermissions":true}
-> Testmodel: , {TodoItem:@id:{E: 1133866475, D: }}, {TodoItem:@id<Comment:@id:{E: 1, D: }}, {Comment:@id:{E: 1, D: }}, {Author:@id:{E: 0, D: }}
```

**Interpretation:**

Operations are logged with their currently percived state:
* `[E]` for existing resources
* `[D]` for deleted resources
* `[U]` for unknown resources

Following the name of the operation the used parameters are displayed.

Additionally the state after the execution of the operation is show listing all the known resource identifiers and their relations.

`TodoItem:@id<Comment:@id` represents a `Comment` which is a child of a `TodoItem`.

**Further References:**
The initial implementation of ObST has been created by Benjamin Kissmann in his master thesis (in German, unfortunately) [Automatisiertes Testen von RESTful Webservices zur Validierung von Claim-basierten Berechtigungskonzepten mittels der OpenAPI-Dokumentation](http://dx.doi.org/10.25673/37346).
