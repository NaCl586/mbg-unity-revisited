grammar TS;

start : decl*;

decl
    : stmt
    | fn_decl_stmt
    | package_decl
    ;

package_decl
    : RW_PACKAGE IDENT '{' fn_decl_stmt+ '}' ';'
    ;

fn_decl_stmt
    : RW_DEFINE IDENT '(' var_list? ')' '{' statement_list '}'
    | RW_DEFINE IDENT OP_COLONCOLON IDENT '(' var_list? ')' '{' statement_list '}'
    ;
    
var_list
    : VAR (',' VAR)*
    ;
    
statement_list
    : stmt*
    ;

stmt
    : if_stmt
    | while_stmt
    | for_stmt
    | datablock_stmt
    | switch_stmt
    | RW_BREAK ';'
    | RW_CONTINUE ';'
    | RW_RETURN ';'
    | RW_RETURN expr ';'
    | expression_stmt ';'
    ;


datablock_stmt
   : RW_DATABLOCK IDENT '(' IDENT parent_block ')'  '{' slot_assign_list '}' ';'
   ;
   
object_decl
   : RW_DECLARE class_name_expr '(' object_name parent_block object_args ')' '{' object_declare_block '}'
   | RW_DECLARE class_name_expr '(' object_name parent_block object_args ')'
   ;

parent_block
   : (':' IDENT)?
   ;

object_name
   : expr?
   ;

object_args
   : (',' expr_list)?
   ;
   
object_declare_block
   : (slot_assign_list | object_decl_list) *
   ;

object_decl_list
   : (object_decl ';') +
   ;

stmt_block
   : '{' statement_list '}'
   | stmt
   ;

switch_stmt
   : RW_SWITCH '(' expr ')' '{' case_block '}'
   | RW_SWITCHSTR '(' expr ')' '{' case_block '}'
   ;

case_block
   : RW_CASE case_expr ':' statement_list
   | RW_CASE case_expr ':' statement_list RW_DEFAULT ':' statement_list
   | RW_CASE case_expr ':' statement_list case_block
   ;

case_expr
   : expr (RW_CASEOR expr)*
   ;

if_stmt
   : RW_IF '(' expr ')' stmt_block
   | RW_IF '(' expr ')' stmt_block RW_ELSE stmt_block
   ;

while_stmt
   : RW_WHILE '(' expr ')' stmt_block
   ;

for_stmt
   : RW_FOR '(' expr ';' expr ';' expr ')' stmt_block
   ;   

expression_stmt
   : stmt_expr
   ;

slot_assign_list
   : slot_assign+
   ;

slot_assign
   : IDENT '=' expr ';'
   | RW_DATABLOCK '=' expr ';'
   | IDENT '[' aidx_expr ']' '=' expr ';'
   ;

aidx_expr
   : expr (',' expr)*
   ;

expr_list_decl
   : expr_list?
   ;

expr_list
   : expr (',' expr)*
   ;

expr
   : IDENT '(' expr_list_decl ')'
   | IDENT OP_COLONCOLON IDENT '(' expr_list_decl ')'
   | expr '.' IDENT '(' expr_list_decl ')'
   | object_decl
   | VAR '=' expr
   | VAR '[' aidx_expr ']' '=' expr
   | VAR assign_op_struct
   | VAR '[' aidx_expr ']' assign_op_struct
   | expr '.' IDENT assign_op_struct
   | expr '.' IDENT '[' aidx_expr ']' assign_op_struct
   | expr '.' IDENT '=' expr
   | expr '.' IDENT '[' aidx_expr ']' '=' expr
   | expr '.' IDENT '=' '{' expr_list '}'
   | expr '.' IDENT '[' aidx_expr ']' '=' '{' expr_list '}'
   | '(' expr ')'
   | expr '^' expr
   | expr '%' expr
   | expr '&' expr
   | expr '|' expr
   | expr '+' expr
   | expr '-' expr
   | expr '*' expr
   | expr '/' expr
   | '-' expr
   | '*' expr
   | expr '?' expr ':' expr
   | expr '<' expr
   | expr '>' expr
   | expr OP_GE expr
   | expr OP_LE expr
   | expr OP_EQ expr
   | expr OP_NE expr
   | expr OP_OR expr
   | expr OP_SHL expr
   | expr OP_SHR expr
   | expr OP_AND expr
   | expr OP_STREQ expr
   | expr OP_STRNE expr
   | expr OP_CAT expr
   | '!' expr
   | '~' expr
   | TAGATOM
   | FLTCONST
   | INTCONST
   | RW_BREAK
   | expr '.' IDENT
   | expr '.' IDENT '[' aidx_expr ']'
   | IDENT
   | STRATOM
   | VAR
   | VAR '[' aidx_expr ']'
   ;

class_name_expr
   : IDENT
   | '(' expr ')'
   ;

assign_op_struct
   : OP_PLUSPLUS
   | OP_MINUSMINUS
   | OP_PLASN expr
   | OP_MIASN expr
   | OP_MLASN expr
   | OP_DVASN expr
   | OP_MODASN expr
   | OP_ANDASN expr
   | OP_XORASN expr
   | OP_ORASN expr
   | OP_SLASN expr
   | OP_SRASN expr
   ;

stmt_expr
   : IDENT '(' expr_list_decl ')'
   | IDENT OP_COLONCOLON IDENT '(' expr_list_decl ')'
   | expr '.' IDENT '(' expr_list_decl ')'
   | object_decl
   | VAR '=' expr
   | VAR '[' aidx_expr ']' '=' expr
   | VAR assign_op_struct
   | VAR '[' aidx_expr ']' assign_op_struct
   | expr '.' IDENT assign_op_struct
   | expr '.' IDENT '[' aidx_expr ']' assign_op_struct
   | expr '.' IDENT '=' expr
   | expr '.' IDENT '[' aidx_expr ']' '=' expr
   | expr '.' IDENT '=' '{' expr_list '}'
   | expr '.' IDENT '[' aidx_expr ']' '=' '{' expr_list '}'
   ;

// Lexer tokens
fragment DIGIT : [0-9];
fragment INTEGER : DIGIT+;
fragment LETTER : [A-Za-z_];
fragment FILECHAR : [A-Za-z_.];
fragment VARMID : [:A-Za-z0-9_];
fragment IDTAIL : [A-Za-z0-9_];
fragment VARTAIL : VARMID* IDTAIL;
fragment HEXDIGIT : [a-zA-F0-9];

STRATOM
    :  '"' SCharSequence? '"'
    ;
TAGATOM
    :  '\'' TCharSequence? '\''
    ;
fragment
SCharSequence
    :   SChar+
    ;
fragment
SChar
    :   ~["\\\r\n]
    |   EscapeSequence
    ;
fragment
TCharSequence
    :   TChar+
    ;
fragment
TChar
    :   ~['\\\r\n]
    |   EscapeSequence
    ;

fragment
EscapeSequence
    :   '\\' ['"?nrt\\]
    |   '\\x' HEXDIGIT+
    |   '\\c' [0-9opr]+
;

OP_EQ : '==' ;
OP_NE : '!=' ;
OP_GE : '>=' ;
OP_LE : '<=' ;
OP_AND : '&&' ;
OP_OR : '||' ;
OP_COLONCOLON : '::' ;
OP_MINUSMINUS : '--' ;
OP_PLUSPLUS : '++' ;
OP_STREQ : '$=' ;
OP_STRNE : '!$=' ;
OP_SHL : '<<' ;
OP_SHR : '>>' ;
OP_PLASN : '+=' ;
OP_MIASN : '-=' ;
OP_MLASN : '*=' ;
OP_DVASN : '/=' ;
OP_MODASN : '%=' ;
OP_ANDASN : '&=' ;
OP_XORASN : '^=' ;
OP_ORASN : '|=' ;
OP_SLASN : '<<=' ;
OP_SRASN : '>>=' ;
OP_CAT : 'NL'
       | 'TAB'
       | 'SPC'
       | '@' ;
RW_CASEOR : 'or' ;
RW_BREAK : 'break' ;
RW_RETURN : 'return' ;
RW_ELSE : 'else' ;
RW_WHILE : 'while' ;
RW_IF : 'if' ;
RW_FOR : 'for' ;
RW_CONTINUE : 'continue' ;
RW_DEFINE : 'function' ;
RW_DECLARE : 'new' ;
RW_DATABLOCK : 'datablock' ;
RW_CASE : 'case' ;
RW_SWITCHSTR : 'switch$' ;
RW_SWITCH : 'switch' ;
RW_DEFAULT : 'default' ;
RW_PACKAGE : 'package' ;

FLTCONST : (INTEGER '.' INTEGER) | (INTEGER ('.' INTEGER)? [eE] [+-]? INTEGER);
VAR : [$%] LETTER VARTAIL*;
IDENT : LETTER IDTAIL*;
INTCONST : '0' [xX] HEXDIGIT+ | INTEGER | 'true' | 'false';

BlockComment
    :   '/*' .*? '*/'
        -> skip
    ;

LineComment
    :   '//' ~[\r\n]*
        -> skip
    ;

WS
    : [ \r\n\t] + -> skip
    ;