module Lexer
    open Tokens
    type LexerState =
        {
            input : string
            mutable position : int
            mutable readPosition : int
            mutable ch : char
        }

    type ComplexTokenType =
        | Letter
        | Digit
        | Illegal

    let lookupIdent ident =
        if ident = "fn" then
            FUNCTION
        else if ident = "let" then
            LET
        else
            IDENT

    let readChar (l: LexerState) =
        let newChar =
            match l.readPosition >= l.input.Length with
            | true -> '\000'
            | false -> l.input.Chars l.readPosition
        l.position <- l.readPosition
        l.readPosition <- l.readPosition + 1
        l.ch <- newChar

    let isLetter(ch: char) =
        let lowerCase = ch.CompareTo('a') >= 0 && ch.CompareTo('z') <= 0
        let upperCase = ch.CompareTo('A') >= 0 && ch.CompareTo('Z') <= 0;
        let underscore = ('_' = ch);
        lowerCase || upperCase || underscore

    let canReadLetter(l: LexerState) =
        isLetter(l.input.Chars(l.position + 1))

    let readIdentifier(l: LexerState) =
        let pos = l.position
        while canReadLetter(l) do
            readChar l
        let literal = l.input.Substring(pos, (l.position - pos + 1))
        let tokenType = lookupIdent literal
        (tokenType, literal)

    let isDigit(ch: char) =
        ch.CompareTo('0') >= 0 && ch.CompareTo('9') <= 0

    let canReadDigit(l: LexerState) =
        isDigit(l.input.Chars(l.position + 1))

    let readNumber(l: LexerState) =
        let pos = l.position
        while canReadDigit(l) do 
            readChar l
        let literal = l.input.Substring(pos, (l.position - pos + 1))
        (INT, literal)
    
    let findComplexTokenType l =
        if isLetter(l.ch) then
            Letter
        else if isDigit(l.ch) then
            Digit
        else
            Illegal

    let nextComplexToken(l: LexerState) =
        match findComplexTokenType(l) with 
        | Letter -> readIdentifier(l)
        | Digit -> readNumber(l)
        | Illegal -> (TokenType.ILLEGAL, l.ch.ToString())

    let skipWhitespace(l: LexerState) =
        while l.ch = ' ' || l.ch = '\t' || l.ch = '\n' || l.ch = '\r' do
            readChar l
        ()

    let nextToken (l: LexerState) =

        skipWhitespace l

        let (tokenType, literal) =
            match l.ch with
            | '=' -> (TokenType.ASSIGN, l.ch.ToString())
            | ';' -> (TokenType.SEMICOLON, l.ch.ToString())
            | '(' -> (TokenType.LPAREN, l.ch.ToString())
            | ')' -> (TokenType.RPAREN, l.ch.ToString())
            | ',' -> (TokenType.COMMA, l.ch.ToString())
            | '+' -> (TokenType.PLUS, l.ch.ToString())
            | '{' -> (TokenType.LBRACE, l.ch.ToString())
            | '}' -> (TokenType.RBRACE, l.ch.ToString())
            | '\000' -> (TokenType.EOF, "")
            | _ -> nextComplexToken l

        let token = { TokenType = tokenType; Literal = literal }
        
        readChar l
        token
    
    let createLexer input =
        let lexer = { input = input; position = 0; readPosition = 0; ch = '\000'}
        readChar lexer
        lexer