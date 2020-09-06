\ forth csv parser by gustav melck, sep 2020
\ vim: fdm=marker

private{  \ {{{

: gthrow  ( ior addr u -- )  2 pick  if  type >r ." ; forth-csv error " r@ . cr r> throw  else  2drop drop  then  ;

0 value ubuf            0 value ubuflen         0 value max-ubuflen

: ubuf-available  ( -- u )  max-ubuflen ubuflen -  ;

: ubuf+  ( addr u -- )  ubuf-available min dup >r ubuf ubuflen + swap cmove  ubuflen r> + to ubuflen  ;
: 0ubuf+  ( addr u -- )  0 to ubuflen  ubuf+  ;

0 value fid

create c 0 c,               0 value prev-c-eol?

0 value next-csv-field#     -1 value (last-csv-field#)

: next-csv-field#0  ( -- )  0 to next-csv-field#  ;
: next-csv-field#+1  ( -- )  next-csv-field# 1+ to next-csv-field#  ;

: next-c  ( -- #chars )  c c@ dup 10 = swap 13 = or to prev-c-eol?  c 1 fid read-file s" next-c error1" gthrow  ;

defer in-quoted-field

: at-quoted-field-end?  ( -- not-eof? )
    r> drop  next-c 1 <>  if  false  else
        c c@ case
            [char] ,  of  next-csv-field#+1 true  endof
            10        of  next-csv-field#0 true  endof
            13        of  next-csv-field#0 true  endof
            drop c 1 ubuf+  true in-quoted-field  \ "true" is to trigger a second r> drop
        endcase
    then  ;

: (in-quoted-field)  ( double-r-drop? -- not-eof? )  \ if the deferred word is called a double r> drop is required
    if  r> drop  then  r> drop  next-c 1 <>  if  false  else
        c c@ case
            [char] "  of  at-quoted-field-end?  endof
            drop c 1 ubuf+ false recurse
        endcase
    then  ;
' (in-quoted-field) is in-quoted-field

: -ubuf-trailing  ( -- )  ubuf ubuflen -trailing to ubuflen drop  ;

: in-bare-field  ( -- not-eof? )
    r> drop  next-c 1 <>  if  -ubuf-trailing false  else
        c c@ case
            [char] ,  of  -ubuf-trailing next-csv-field#+1 true  endof
            10        of  -ubuf-trailing next-csv-field#0 true  endof
            13        of  -ubuf-trailing next-csv-field#0 true  endof
            drop c 1 ubuf+  recurse
        endcase
    then  ;

: (before-field)  ( -- not-eof? )
    r> drop  next-c 1 <>  if  false  else
        c c@ case
            [char] "  of  true in-quoted-field  endof
            [char] ,  of  next-csv-field#+1 true  endof
            10        of  prev-c-eol? 0=  if  next-csv-field#0 true  else  recurse  then  endof
            13        of  prev-c-eol? 0=  if  next-csv-field#0 true  else  recurse  then  endof
            bl        of  recurse  endof
            drop c 1 ubuf+  in-bare-field
        endcase
    then  ;
: before-field  ( -- not-eof? )  (before-field)  ;

}private  \ }}}

: with-csv-file-id  ( fid -- )  to fid  ;

: read-csv-field  ( addr u -- u' not-eof? )
    to max-ubuflen to ubuf  0 to ubuflen  next-csv-field# to (last-csv-field#)  before-field  ubuflen swap  ;

: last-csv-field#  ( -- u )  (last-csv-field#)  ;

privatize

\ tests {{{
create buffer 128 chars allot

: type-buffer  ( u -- )  buffer swap type  ;

: (test)  ( in-loop? -- )
    if  r> drop  then
    buffer 128 read-csv-field 0=  if  drop  else
        last-csv-field# . ." :" type-buffer ." ;" cr  true recurse
    then  ;
: test  ( -- )
    s" test.csv" r/o open-file throw dup >r with-csv-file-id  false (test)  
    r> close-file throw  ;

test .s
\ }}}

