module Evaluator

    let trueBooleanObject = new Object.Boolean(true)
    let falseBooleanObject = new Object.Boolean(false)
    let nullObject = new Object.Null()

    let toObj obj = obj :> Object.Object

    let toSomeObj obj = Some (obj :> Object.Object)
    let toSome obj = Some obj

    
    let canDowncastToFunction (s: Object.Object) =
        match s with 
        | :? Object.Function as func -> true
        | _ -> false
    let canDowncastToBuiltin (s: Object.Object) =
        match s with 
        | :? Object.Builtin as func -> true
        | _ -> false
    let canDowncastToReturn (s: Object.Object) =
        match s with 
        | :? Object.Return as retr -> true
        | _ -> false

    let isTruthy (obj : Object.Object)  =
        match obj.Type() with
        | Object.NULL -> false
        | Object.BOOLEAN ->
            let bool = obj :?> Object.Boolean
            bool.value
        | _ -> true

    let getTypeStr obj =
        (obj :> Object.Object).Type().ToString()

    let newError message =
        new Object.Error(message)

    let newErrorObj message =
        newError message
        |> toObj

    let isError (obj: Object.Object) =
        obj.Type() = Object.ObjectType.ERROR

    let isSomeError (obj: Object.Object option) =
        if obj.IsSome then
            isError obj.Value
        else
            false

    let nativeBoolToBooleanObject input =
        if input then trueBooleanObject
        else falseBooleanObject

    let evalBangOperatorExpression (right: Object.Object) =
        match right.Type() with
        | Object.ObjectType.BOOLEAN ->
            let bool = right :?> Object.Boolean

            nativeBoolToBooleanObject (not bool.value)
        | Object.ObjectType.NULL -> trueBooleanObject
        | _ -> falseBooleanObject

    let evalMinusPrefixOperatorExpression (right: Object.Object) =
        match right.Type() with
        | Object.ObjectType.INTEGER ->
            let int = right :?> Object.Integer

            let reversedInt = int.value * -1L

            let reversedObj = new Object.Integer(reversedInt)

            (reversedObj :> Object.Object)
        | _ -> 
            getTypeStr right
            |> sprintf "unknown operator: -%s"
            |> newError
            |> toObj


    let evalIntegerInfixExpression operator (left: Object.Integer) (right: Object.Integer) =
        match operator with
        | "+" -> 
            new Object.Integer(left.value + right.value)
            |> toObj
        | "-" -> 
            new Object.Integer(left.value - right.value)
            |> toObj
        | "*" -> 
            new Object.Integer(left.value * right.value)
            |> toObj
        | "/" -> 
            new Object.Integer(left.value / right.value)
            |> toObj
        | "<" ->
            nativeBoolToBooleanObject(left.value < right.value)
            |> toObj
        | ">" ->
            nativeBoolToBooleanObject(left.value > right.value)
            |> toObj
        | "==" ->
            nativeBoolToBooleanObject(left.value = right.value)
            |> toObj
        | "!=" -> 
            nativeBoolToBooleanObject(left.value <> right.value)
            |> toObj
        | _ -> 
            let leftStr = getTypeStr left
            let rightStr = getTypeStr right

            sprintf "unknown operator: %s %s %s" leftStr operator rightStr
            |> newError
            |> toObj

    let evalBooleanInfixExpression operator (left: Object.Boolean) (right: Object.Boolean) =
        match operator with
        | "==" ->
            new Object.Boolean(left.value = right.value)
            |> toObj
        | "!=" -> 
            new Object.Boolean(left.value <> right.value)
            |> toObj
        | _ ->
            let leftStr = getTypeStr left
            let rightStr = getTypeStr right

            sprintf "unknown operator: %s %s %s" leftStr operator rightStr
            |> newError
            |> toObj

    let evalStringInfixExpression operator (left: Object.Str) (right: Object.Str) =
        match operator with
        | "+" ->
            new Object.Str(left.value + right.value)
            |> toObj
        | _ ->
            let leftStr = getTypeStr left
            let rightStr = getTypeStr right

            sprintf "unknown operator: %s %s %s" leftStr operator rightStr
            |> newError
            |> toObj


    let evalPrefixExpression operator right =
        match operator with
        | "!" ->
            let booleanObject = evalBangOperatorExpression right
            booleanObject :> Object.Object
        | "-" ->
            evalMinusPrefixOperatorExpression right
        | _ -> 
            getTypeStr right
            |> sprintf "unknown operator: %s%s" operator
            |> newError
            |> toObj

    let evalInfixExpression operator (left: Object.Object) (right: Object.Object) =
        match left.Type(), right.Type() with
        | Object.ObjectType.INTEGER, Object.ObjectType.INTEGER ->
            let leftInt = left :?> Object.Integer
            let rightInt = right :?> Object.Integer
            evalIntegerInfixExpression operator leftInt rightInt
        | Object.ObjectType.BOOLEAN, Object.ObjectType.BOOLEAN ->
            let leftBool = left :?> Object.Boolean
            let rightBool = right :?> Object.Boolean
            evalBooleanInfixExpression operator leftBool rightBool
        | Object.ObjectType.STRING, Object.ObjectType.STRING ->
            let leftStr = left :?> Object.Str
            let rightStr = right :?> Object.Str
            evalStringInfixExpression operator leftStr rightStr
        | _, _ ->             
            let leftStr = getTypeStr left
            let rightStr = getTypeStr right
            let mutable errorString = ""

            if left.Type() <> right.Type() then
                errorString <- (sprintf "type mismatch: %s %s %s" leftStr operator rightStr)
                ()
            else 
                errorString <- sprintf "unknown operator: %s %s %s" leftStr operator rightStr
                ()
            errorString
            |> newError
            |> toObj
    
    let evalIdentifier (identifier: string) (env: Object.Environment) =
        let value = env.Get identifier

        match value with
        | Some v ->
            v
        | None ->

            if Builtins.builtinsMap.ContainsKey identifier then
                Builtins.builtinsMap.[identifier]
                |> toObj
            else
                sprintf "identifier not found: %s" identifier
                |> newError
                |> toObj

    let extendFunctionEnv (fn: Object.Function) (args: Object.Object[]) =
        let env = new Object.Environment(Some fn.env)

        for i = 0 to (fn.parameters.Length - 1) do
            let param = fn.parameters.[i].value
            env.Set param args.[i]

        env

    let unwrapReturnValue (obj: Object.Object option) =
        if obj.IsSome && canDowncastToReturn obj.Value then
            let returnStmt = obj.Value :?> Object.Return
            Some returnStmt.value
        else
            obj

    let evalArrayIndexExpression (left: Object.Object) (index: Object.Object) =
        let index = (index :?> Object.Integer).value

        let arrayObj = (left :?> Object.Array)

        let max = int64 arrayObj.elements.Length

        if index > int64 System.Int32.MaxValue then
            sprintf "Array index greater than Int32.MaxValue: %d" index
            |> newErrorObj 
        else if index < 0L || index > (max - 1L) then
            nullObject
            |> toObj
        else
            let smallIndex = int32 index
            arrayObj.elements.[smallIndex]

    let evalHashIndexExpresson (left: Object.Object) (index: Object.Object) =
        let ind = (index :?> Object.Integer)

        let hash = (left :?> Object.Hash)

        match hash.Get ind with
        | Some v ->
            v
        | None ->
            nullObject
            |> toObj
        

    let evalIndexExpression (left: Object.Object) (index: Object.Object) =
        match left.Type(), index.Type() with
        | Object.ObjectType.ARRAY, Object.ObjectType.INTEGER ->
            evalArrayIndexExpression left index
        | Object.ObjectType.HASH, Object.ObjectType.INTEGER ->
            evalHashIndexExpresson left index
        | _, _ ->
            left.Type().ToString()
            |> sprintf "index operator not support: %s"
            |> newErrorObj


    let rec eval (node: Ast.Node) (env: Object.Environment) =
        match node.AType() with 
        | Ast.Program -> 
            let program = node :?> Ast.Program
            evalProgram program.statements env
        | Ast.ExpressionStatement ->
            let exprStmt = node :?> Ast.ExpressionStatement
            eval exprStmt.expression env
        | Ast.IntegerLiteral ->
            let intLit = node :?> Ast.IntegerLiteral
            let int = new Object.Integer(intLit.value)
            Some (int :> Object.Object)
        | Ast.Boolean ->
            let boolLit = node :?> Ast.Boolean
            let boolObj = 
                if boolLit.value then trueBooleanObject
                else falseBooleanObject
            Some (boolObj :> Object.Object)
        | Ast.PrefixExpression ->
            let preExpr = node :?> Ast.PrefixExpression
            let right = eval preExpr.right env

            if right.IsSome then
                let rValue = right.Value

                match isError rValue with
                | true ->
                    Some rValue
                | false ->
                    let result = evalPrefixExpression preExpr.operator rValue
                    Some result
            else None
        | Ast.InfixExpression ->
            let inExpr = node :?> Ast.InfixExpression

            let left = eval inExpr.left env
            let right = eval inExpr.right env

            match left, right with
            | Some l, Some r ->

                match isError(l), isError(r) with
                | true, _ ->
                    Some l
                | false, true ->
                    Some r
                | false, false ->
                    let result = evalInfixExpression inExpr.operator l r
                    Some result
            | _, _ -> None
        | Ast.BlockStatement ->
            let block = node :?> Ast.BlockStatement
            evalBlockStatement block env
        | Ast.IfExpression ->
            let ifExpr = node :?> Ast.IfExpression
            evalIfExpression ifExpr env
        | Ast.ReturnStatement ->
            let rtnStmt = node :?> Ast.ReturnStatement
            let result = eval rtnStmt.returnValue env

            if result.IsSome then
                let value = result.Value
                match isError value with
                | true ->
                    toSomeObj value
                | false -> 
                    let returnValue = new Object.Return(value)
                    toSomeObj returnValue
            else 
                None
        | Ast.LetStatement ->
            let letStmt = node :?> Ast.LetStatement
            let result = eval letStmt.value env

            if result.IsSome then
                let value = result.Value

                match isError value with
                | true ->
                    toSomeObj value
                | false ->
                    env.Set letStmt.name.value value
                    None
            else
                None
        | Ast.Identifier ->
            let iden = node :?> Ast.Identifier

            evalIdentifier iden.value env
            |> toSomeObj
        | Ast.FunctionLiteral ->
            let func = node :?> Ast.FunctionLiteral

            let param = func.parameters
            let body = func.body

            new Object.Function(param, body, env)
            |> toSomeObj
        | Ast.CallExpression ->
            let callExpr = node :?> Ast.CallExpression

            let func = eval callExpr.func env

            if func.IsSome then
                let funcValue = func.Value

                match isError funcValue with
                | true ->
                    toSomeObj funcValue
                | false ->
                    let args: Object.Object array = evalExpressions callExpr.arguments env

                    if args.Length = 1 && isError args.[0] then
                        toSomeObj args.[0]
                    else
                        applyFunction funcValue args
            else
                None
        | Ast.StringLiteral ->
            let str = node :?> Ast.StringLiteral
            new Object.Str(str.value)
            |> toSomeObj
        | Ast.ArrayLiteral ->
            let arr = node :?> Ast.ArrayLiteral

            let elements = evalExpressions arr.elements env

            if elements.Length = 1 && isError elements.[0] then
                elements.[0]
                |> toSomeObj
            else
                new Object.Array(elements)
                |> toSomeObj
        | Ast.IndexExpression ->
            let indexExpr = node :?> Ast.IndexExpression

            let leftSome = eval indexExpr.left env

            if leftSome.IsSome then
                let left = leftSome.Value

                if isError left then
                    toSomeObj left
                else
                    let indexSome = eval indexExpr.index env

                    if indexSome.IsSome then
                        let index = indexSome.Value

                        if isError index then
                            toSomeObj index
                        else
                            evalIndexExpression left index
                            |> toSome
                    else
                        None
            else
                None
        | Ast.HashLiteral ->
            let hash = node :?> Ast.HashLiteral
            
            evalHashLiteral hash env
            |> toSomeObj

    and evalProgram (stmts: Ast.Statement[]) (env: Object.Environment) =
        let mutable result : Object.Object option = None
        let mutable earlyReturn : Object.Object option = None

        for stmt in stmts do
            result <- eval stmt env

            //return early if return or error
            if result.IsSome  && earlyReturn.IsNone then
                let rs = result.Value
                match rs.Type() with
                | Object.ObjectType.RETURN ->
                    earlyReturn <- Some rs
                    ()
                | Object.ObjectType.ERROR ->
                    earlyReturn <- Some rs
                    ()
                | _ -> ()
                    
                ()
            else 
                ()
        
        match earlyReturn with
        | Some er ->
            match er.Type() with
            | Object.ObjectType.RETURN ->
                let rtrStmt = (er :?> Object.Return)
                Some rtrStmt.value
            | Object.ObjectType.ERROR ->
                Some er
            | _ -> result
        | None -> result
    
    and evalBlockStatement (block: Ast.BlockStatement) (env: Object.Environment) =
        let mutable result : Object.Object option = None
        let mutable earlyReturn : Object.Object option = None
        let mutable keepGoing = true
        let mutable stmtCount = 0

        while stmtCount < block.statements.Length && keepGoing do
            let stmt = block.statements.[stmtCount]
            result <- (eval stmt env)

            match result with
            | Some r ->
                if earlyReturn.IsNone && (r.Type() = Object.RETURN || r.Type() = Object.ERROR)  then
                    earlyReturn <- Some r
                    keepGoing <- false
                    ()
                else
                    ()
            | None ->
                ()
            
            stmtCount <- stmtCount + 1
        
        if earlyReturn.IsSome then
            earlyReturn
        else
            result

    and evalIfExpression (ifExpr: Ast.IfExpression) (env: Object.Environment) =
        let condition = eval ifExpr.condition env

        match condition with
        | Some c ->
            if isError c then
                Some c
            else if isTruthy c then
                eval ifExpr.consequence env
            else if ifExpr.alternative.IsSome then
                eval ifExpr.alternative.Value env
            else Some (nullObject |> toObj)
        | None -> Some (nullObject |> toObj)

    and evalExpressions (exps: Ast.Expression[]) (env: Object.Environment) =
        let results = new ResizeArray<Object.Object>()
        let errorResult = new ResizeArray<Object.Object>()
        let mutable exprIndex = 0
        let mutable keepGoing = true

        while exprIndex < exps.Length && keepGoing do
            let exp = exps.[exprIndex]
            let evaluated = eval exp env

            match evaluated with
            | Some ev ->
                if isError ev && errorResult.Count = 0 then
                    errorResult.Add(ev)
                    keepGoing <- false
                    ()
                else
                    results.Add(ev)
                    ()
            | None ->
                ()
            exprIndex <- exprIndex + 1
        
        if errorResult.Count > 0 then
            errorResult.ToArray()
        else
            results.ToArray()
    
    and applyFunction (fn: Object.Object) (args: Object.Object[]) =
        if canDowncastToFunction fn then
            let func = fn :?> Object.Function

            let extendedEnv = extendFunctionEnv func args
            let evaluated = eval func.body extendedEnv

            unwrapReturnValue evaluated
        else if canDowncastToBuiltin fn then
            let builtin = fn :?> Object.Builtin

            builtin.fn args
            |> toSomeObj
        else
            let typeStr = fn.Type().ToString()
            sprintf "not a function: %s" typeStr
            |> newError 
            |> toSomeObj
    
    and evalHashLiteral (hash: Ast.HashLiteral) (env: Object.Environment) =
        let mutable pairs = Map.empty<Object.Integer, Object.Object>
        let mutable error: Object.Object option = None
        
        for e in hash.pairs do
            let oldKey = e.Key

            let newKey = eval oldKey env

            if isSomeError newKey then
                error <- newKey
                ()
            else
                let key = newKey.Value :?> Object.Integer

                let value = eval e.Value env

                if isSomeError value && error.IsNone then
                    error <- value
                    ()
                else
                    pairs <- pairs.Add(key, value.Value)
                    ()
        
        if error.IsSome then
            error.Value
            |> toObj
        else
            new Object.Hash(pairs)
            |> toObj

    let evaluate (node: Ast.Node) =
        let env = new Object.Environment(None)
        eval node env