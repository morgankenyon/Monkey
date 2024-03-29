module EvaluatorTests

open Xunit
open Lexer
open Parser
open System

let canDowncastToInteger (s: Object.Object) =
    match s with 
    | :? Object.Integer as int -> true
    | _ -> false
let canDowncastToBoolean (s: Object.Object) =
    match s with 
    | :? Object.Boolean as int -> true
    | _ -> false
let canDowncastToNull (s: Object.Object) =
    match s with 
    | :? Object.Null as int -> true
    | _ -> false
let canDowncastToReturn (obj : Object.Object) =
    match obj with 
    | :? Object.Return as rtr -> true
    | _ -> false
let canDowncastToError (obj : Object.Object) =
    match obj with 
    | :? Object.Error as err -> true
    | _ -> false
let canDowncastToFunction (obj : Object.Object) =
    match obj with 
    | :? Object.Function as fnc -> true
    | _ -> false
let canDowncastToStr (obj : Object.Object) =
    match obj with 
    | :? Object.Str as str -> true
    | _ -> false
let canDowncastToArray (obj : Object.Object) =
    match obj with 
    | :? Object.Array as arr -> true
    | _ -> false
let canDowncastToHash (obj : Object.Object) =
    match obj with 
    | :? Object.Hash as hash -> true
    | _ -> false

let toInteger input =
    new Object.Integer(input)
 
let testEval input =
    let lexer = createLexer input
    let parser = createParser lexer
    let program = parseProgram parser

    Evaluator.evaluate program

let testIntegerObject (obj: Object.Object option) expected =
    Assert.True(obj.IsSome, "IsNone")

    let someObj = obj.Value
    let objType = someObj.GetType().ToString()

    Assert.True(canDowncastToInteger someObj, (sprintf "Cannot downcast %s to Integer" objType))

    let int = someObj :?> Object.Integer

    Assert.Equal(expected, int.value)

let testStringLiteral (obj: Object.Object option) expected =
    Assert.True(obj.IsSome, "IsNone")

    let someObj = obj.Value
    let objType = someObj.GetType().ToString()

    Assert.True(canDowncastToStr someObj, (sprintf "Cannot downcast %s to Str" objType))

    let int = someObj :?> Object.Str

    Assert.Equal(expected, int.value)


let testBooleanObject (obj: Object.Object option) expected =
    Assert.True(obj.IsSome, "IsNone")

    let someObj = obj.Value

    Assert.True(canDowncastToBoolean someObj, "Cannot downcast")

    let bool = someObj :?> Object.Boolean

    let msg = someObj.Inspect()

    Assert.True((expected = bool.value), msg)

let testNullObject (obj: Object.Object option) =
    Assert.True(obj.IsSome, "IsNone")

    let someObj = obj.Value
    let objType = someObj.GetType().ToString()
    Assert.True(canDowncastToNull someObj, (sprintf "Cannot downcast %s to Null" objType))

let testReturnValue (obj: Object.Object option) expected =
    Assert.True(obj.IsSome, "IsNone")

    let someObj = obj.Value

    Assert.True(canDowncastToReturn someObj, "Cannot downcast to return")

    let rtr = someObj :?> Object.Return

    Assert.True(canDowncastToInteger rtr.value, "Cannot downcast to integer")

    let int = rtr.value :?> Object.Integer

    Assert.Equal(expected, int.value)

let testErrorObject (obj: Object.Object option) expectedError =
    Assert.True(obj.IsSome, "IsNone")

    let someObj = obj.Value

    let objType = someObj.GetType().ToString()

    Assert.True(canDowncastToError someObj, sprintf "Cannot downcast %s to error" objType)

    let error = someObj :?> Object.Error

    Assert.Equal(expectedError, error.message)

let testArrayObject (obj: Object.Object option) expectedLength =
    Assert.True(obj.IsSome, "IsNone")

    let someObj = obj.Value

    let objType = someObj.GetType().ToString()

    Assert.True(canDowncastToArray someObj, sprintf "Cannot downcast %s to error" objType)

    let arr = someObj :?> Object.Array

    Assert.Equal(expectedLength, arr.elements.Length)

[<Theory>]
[<InlineData("5", 5L)>]
[<InlineData("10", 10L)>]
[<InlineData("-5", -5L)>]
[<InlineData("-10", -10L)>]
[<InlineData("5 + 5 + 5 + 5 - 10", 10)>]
[<InlineData("2 * 2 * 2 * 2 * 2", 32)>]
[<InlineData("-50 + 100 + -50", 0)>]
[<InlineData("5 * 2 + 10", 20)>]
[<InlineData("5 + 2 * 10", 25)>]
[<InlineData("20 + 2 * -10", 0)>]
[<InlineData("50 / 2 * 2 + 10", 60)>]
[<InlineData("2 * (5 + 10)", 30)>]
[<InlineData("3 * 3 * 3 + 10", 37)>]
[<InlineData("3 * (3 * 3) + 10", 37)>]
[<InlineData("(5 + 10 * 2 + 15 / 3) * 2 + -10", 50)>]

let ``Can Test and Eval Integers`` input expected =

    let evaluated = testEval input

    testIntegerObject evaluated expected
    

[<Theory>]
[<InlineData("true", true)>]
[<InlineData("false", false)>]
[<InlineData("1 < 2", true)>]
[<InlineData("1 > 2", false)>]
[<InlineData("1 < 1", false)>]
[<InlineData("1 > 1", false)>]
[<InlineData("1 == 1", true)>]
[<InlineData("1 != 1", false)>]
[<InlineData("1 == 2", false)>]
[<InlineData("1 != 2", true)>]
[<InlineData("true == true", true)>]
[<InlineData("false == false", true)>]
[<InlineData("true == false", false)>]
[<InlineData("true != false", true)>]
[<InlineData("false != true", true)>]
[<InlineData("(1 < 2) == true", true)>]
[<InlineData("(1 < 2) == false", false)>]
[<InlineData("(1 > 2) == true", false)>]
[<InlineData("(1 > 2) == false", true)>]
let ``Can Test and Eval Booleans`` input expected =

    let evaluated = testEval input

    testBooleanObject evaluated expected
    
[<Theory>]
[<InlineData("!true", false)>]
[<InlineData("!false", true)>]
[<InlineData("!5", false)>]
[<InlineData("!!true", true)>]
[<InlineData("!!false", false)>]
[<InlineData("!!5", true)>]
let ``Can test bang operator`` input expected =
    let evaluated = testEval input

    testBooleanObject evaluated expected

[<Theory>]
[<InlineData("if (true) { 10 }", 10L)>]
[<InlineData("if (false) { 10 }", null)>]
[<InlineData("if (1) { 10 }", 10L)>]
[<InlineData("if (1 < 2) { 10 }", 10L)>]
[<InlineData("if (1 > 2) { 10 }", null)>]
[<InlineData("if (1 > 2) { 10 } else { 20 }", 20L)>]
[<InlineData("if (1 < 2) { 10 } else { 20 }", 10L)>]
let ``Can test if else expressions`` input (expected: int64 Nullable ) =
    let evaluated = testEval input

    match expected.HasValue with 
    | true ->
        testIntegerObject evaluated expected.Value
    | false -> 
        testNullObject evaluated

[<Theory>]
[<InlineData("return 10;", 10L)>]
[<InlineData("return 10; 9;", 10L)>]
[<InlineData("return 2 * 5; 9;", 10L)>]
[<InlineData("9; return 2 * 5; 9;", 10L)>]
[<InlineData("if (10 > 1) {
if (10 > 1) {
return 10;
}
return 1;
}", 10L)>]
let ``Can test evaluation of return statements`` input (expected: int64) =
    let evaluated = testEval input

    testIntegerObject evaluated expected

[<Theory>]
[<InlineData("5 + true;","type mismatch: INTEGER + BOOLEAN")>]
[<InlineData("5 + true; 5;","type mismatch: INTEGER + BOOLEAN")>]
[<InlineData("-true","unknown operator: -BOOLEAN")>]
[<InlineData("true + false;","unknown operator: BOOLEAN + BOOLEAN")>]
[<InlineData("\"hello\" - \"world\";","unknown operator: STRING - STRING")>]
[<InlineData("5; true + false; 5","unknown operator: BOOLEAN + BOOLEAN")>]
[<InlineData("if (10 > 1) { true + false; }","unknown operator: BOOLEAN + BOOLEAN")>]
[<InlineData("if (10 > 1) { if (10 > 1) { return true + false; } return 1; }","unknown operator: BOOLEAN + BOOLEAN")>]
[<InlineData("foobar", "identifier not found: foobar")>]
let ``Can test error handling`` input expectedError =
    let evaluated = testEval input

    testErrorObject evaluated expectedError

[<Theory>]
[<InlineData("let a = 5; a;", 5L)>]
[<InlineData("let a = 5 * 5; a;", 25L)>]
[<InlineData("let a = 5; let b = a; b;", 5L)>]
[<InlineData("let a = 5; let b = a; let c = a + b + 5; c;", 15L)>]
let ``Can test let statements`` input expected =
    let evaluated = testEval input

    testIntegerObject evaluated expected

[<Fact>]
let ``Can test function object`` () =
    let input = "fn(x) { x + 2; };"

    let evaluated = testEval input

    Assert.True(evaluated.IsSome, "IsNone")

    let someObj = evaluated.Value

    Assert.True(canDowncastToFunction someObj, "Cannot downcast to function")

    let func = someObj :?> Object.Function

    Assert.Equal(1, func.parameters.Length)

    let param = (func.parameters.[0] :> Ast.Expression).Str()
    Assert.Equal("x", param)

    let bodyStr = (func.body :> Ast.Statement).Str()

    Assert.Equal("(x + 2)", bodyStr)

[<Theory>]
[<InlineData("let identity = fn(x) { x; }; identity(5);", 5L)>]
[<InlineData("let identity = fn(x) { return x; }; identity(5);", 5L)>]
[<InlineData("let double = fn(x) { x * 2; }; double(5);", 10L)>]
[<InlineData("let add = fn(x, y) { x + y; }; add(5, 5);", 10L)>]
[<InlineData("let add = fn(x, y) { x + y; }; add(5 + 5, add(5, 5));", 20L)>]
[<InlineData("fn(x) { x; }(5)", 5L)>]
let ``CanTestFunctionApplications`` input expected =
    let evaluated = testEval input

    testIntegerObject evaluated expected

[<Fact>]
let ``Can test closures`` () =
    let input = "let newAdder = fn(x) {
fn(y) { x + y };
};
let addTwo = newAdder(2);
addTwo(2);"

    let evaluated = testEval input

    testIntegerObject evaluated 4L

[<Fact>]
let ``Can test string literal`` () =
    let input = "\"Hello world!\""

    let evaluated = testEval input

    testStringLiteral evaluated "Hello world!"

[<Fact>]
let ``Can test string concatenation`` () =
    let input = "\"Hello\" + \" \" + \"world!\""

    let evaluated = testEval input

    testStringLiteral evaluated "Hello world!"

[<Theory>]
[<InlineData("len(\"\")", 0L)>]
[<InlineData("len(\"four\")", 4L)>]
[<InlineData("len(\"hello world\")", 11L)>]
[<InlineData("len([1, 2, 3])", 3L)>]
[<InlineData("len([])", 0L)>]
[<InlineData("first([1, 2, 3])", 1L)>]
[<InlineData("first([])", null)>]
[<InlineData("last([1,2,3,4])", 4L)>]
[<InlineData("last([4])", 4L)>]
[<InlineData("last([])", null)>]
let ``Can test len built in function success`` input (expected: int64 Nullable) =
    let evaluated = testEval input

    match expected.HasValue with 
    | true ->
        testIntegerObject evaluated expected.Value
    | false -> 
        testNullObject evaluated

[<Theory>]
[<InlineData("rest([1,2,3,4])", 3)>]
[<InlineData("rest(rest([1,2,3,4]))", 2)>]
[<InlineData("rest(rest(rest([1,2,3,4])))", 1)>]
[<InlineData("rest(rest(rest(rest([1,2,3,4]))))", 0)>]
[<InlineData("rest(rest(rest(rest(rest([1,2,3,4])))))", null)>]
let ``Can test rest of arrays`` input (expectedLength: int Nullable) =
    let evaluated = testEval input

    match expectedLength.HasValue with
    | true ->
        testArrayObject evaluated expectedLength.Value
    | false ->
        testNullObject evaluated

[<Fact>]
let ``Can test push array function`` () =
    let input = "let a = [1,2,3,4];
    let b = push(a, 5);
    b"

    let evaluated = testEval input
    testArrayObject evaluated 5

[<Theory>]
[<InlineData("len(1)", "argument to \"len\" not supported, got INTEGER")>]
[<InlineData("len(\"one\", \"two\")", "wrong number of arguments. got=2, want=1")>]
[<InlineData("first(1)", "argument to \"first\" must be ARRAY, got INTEGER")>]
[<InlineData("first(\"one\", \"two\")", "wrong number of arguments. got=2, want=1")>]
[<InlineData("last(1)", "argument to \"last\" must be ARRAY, got INTEGER")>]
[<InlineData("last(\"one\", \"two\")", "wrong number of arguments. got=2, want=1")>]
[<InlineData("rest(1)", "argument to \"rest\" must be ARRAY, got INTEGER")>]
[<InlineData("rest(\"one\", \"two\")", "wrong number of arguments. got=2, want=1")>]
let ``Can test len built in function errors`` input expected =
    let evaluated = testEval input

    testErrorObject evaluated expected

[<Fact>]
let ``Can test array literals`` () =
    let input = "[1, 2 * 2, 3 + 3]"

    let evaluated = testEval input

    Assert.True(evaluated.IsSome)

    let obj = evaluated.Value

    Assert.True(canDowncastToArray obj)

    let arr = obj :?> Object.Array

    Assert.Equal(3, arr.elements.Length)

    testIntegerObject (Some arr.elements.[0]) 1L
    testIntegerObject (Some arr.elements.[1]) 4L
    testIntegerObject (Some arr.elements.[2]) 6L

[<Theory>]
[<InlineData("[1, 2, 3][0]",1L)>]
[<InlineData("[1, 2, 3][1]",2L)>]
[<InlineData("[1, 2, 3][2]",3L)>]
[<InlineData("let i = 0; [1][i];",1L)>]
[<InlineData("[1, 2, 3][1 + 1];",3L)>]
[<InlineData("let myArray = [1, 2, 3]; myArray[2];",3L)>]
[<InlineData("let myArray = [1, 2, 3]; myArray[0] + myArray[1] + myArray[2];",6L)>]
[<InlineData("let myArray = [1, 2, 3]; let i = myArray[0]; myArray[i]",2L)>]
[<InlineData("[1, 2, 3][3]", null)>]
[<InlineData("[1, 2, 3][-1]",null)>]
let ``Can test array index expression`` input (expected: int64 Nullable) =
    let evaluated = testEval input

    match expected.HasValue with 
    | true ->
        testIntegerObject evaluated expected.Value
    | false -> 
        testNullObject evaluated

[<Fact>]
let ``Can grab hash value`` () =
    let input = "let h = {
        1: 100,
    };
    h[1]"

    let evaluated = testEval input

    testIntegerObject evaluated 100L

[<Fact>]
let ``Can grab hash value of string`` () =
    let input = "let h = {
        1: \"test baby\",
    };
    h[1]"

    let evaluated = testEval input

    testStringLiteral evaluated "test baby"

[<Fact>]
let ``Can hash return null for bad key`` () =
    let input = "let h = {
        1: \"test baby\",
    };
    h[2]"

    let evaluated = testEval input

    testNullObject evaluated


[<Fact>]
let ``Can test hash literals`` () =
   let input = "{
       1: \"one\",
       2: \"two\",
       3: \"three\",
       4: \"four\",
       5: \"five\",
       6: \"six\",
   }"

   let evaluatedSome = testEval input
    
   Assert.True(evaluatedSome.IsSome, "IsNone")

   let evaluated = evaluatedSome.Value
   let objType = evaluated.GetType().ToString()

   Assert.True(canDowncastToHash evaluated, (sprintf "Cannot downcast %s to Hash" objType))

   let hash = evaluated :?> Object.Hash

   let expectedMap =
       Map.empty
           .Add(toInteger 1L, "one")
           .Add(toInteger 2L, "two")
           .Add(toInteger 3L, "three")
           .Add(toInteger 4L, "four")
           .Add(toInteger 5L, "five")
           .Add(toInteger 6L, "six")
    
   for e in expectedMap do
       let key = e.Key

       let objSome = hash.Get key

       Assert.True(objSome.IsSome, (sprintf "Did not find key: %d" key.value))

       let obj = objSome.Value

       Assert.Equal(e.Value, obj.Inspect())

[<Fact>]
let ``Can test fibonacci`` () =
    let input = "let fib = fn (x) {
    if (x < 1) {
        return 0;
    }
    
    if (x == 1) {
        return 1;
    }

    return fib(x - 1) + fib(x - 2);
};

fib(3)"

    let evaluated = testEval input

    testIntegerObject evaluated 2L