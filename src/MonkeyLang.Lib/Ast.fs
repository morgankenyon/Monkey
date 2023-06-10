module Ast
    type Node =
        abstract member TokenLiteral : unit -> string
        abstract member Str : unit -> string
    
    type Statement =
        inherit Node
        abstract member statementNode: unit -> unit

    type Expression =
        inherit Node
        abstract member expressionNode: unit -> unit
    
    type Program(statements : Statement []) =
        member this.statements = statements
        interface Node with 
            member this.TokenLiteral () =
                match this.statements.Length with 
                | 0 -> ""
                | _ -> 
                    let firstStatement = this.statements.[0]
                    (firstStatement :> Node).TokenLiteral()
            member this.Str () =
                this.statements
                    |> Array.map (fun s -> s.Str())
                    |> Array.reduce (fun a b -> a + "\n" + b)
    
    type Identifier(token: Tokens.Token, value: string) =
        member this.token = token
        member this.value = value
        interface Expression with
            member this.TokenLiteral () = token.Literal
            member this.expressionNode () = ()
            member this.Str () = value

    type LetStatement(token: Tokens.Token, name: Identifier, value: Expression) =
        member this.token = token
        member this.name = name
        member this.value = value
        interface Statement with
            member this.TokenLiteral () = token.Literal
            member this.statementNode () = ()
            member this.Str () =
                sprintf "%s %s = %s;" (this.token.Literal) ((this.name :> Expression).Str()) (this.value.Str())

    type ReturnStatement(token: Tokens.Token, returnValue: Expression) =
        member this.token = token
        member this.returnValue = returnValue
        interface Statement with
            member this.TokenLiteral () = token.Literal
            member this.statementNode () = ()
            member this.Str () =
                sprintf "%s %s;" (this.token.Literal) (this.returnValue.Str())

    type ExpressionStatement(token: Tokens.Token, expression: Expression) =
        member this.token = token
        member this.expression = expression
        interface Statement with
            member this.TokenLiteral () = token.Literal
            member this.statementNode () = ()
            member this.Str () = this.expression.Str()

