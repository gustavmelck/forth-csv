\ forth csv parser by gustav melck, sep 2020
\ vim: fdm=marker

marker reset

private{  \ {{{

: gthrow  ( ior addr u -- )  2 pick  if  type >r ." ; forth-csv error " r@ . cr r> throw  else  2drop drop  then  ;

create buffer 128 chars allot           0 value bufferlen

: buffer+  ( addr u -- )  dup >r buffer bufferlen + swap cmove  bufferlen r> + to bufferlen  ;
: 0buffer+  ( addr u -- )  0 to bufferlen  buffer+  ;

0 value fid

create c 1 chars allot

: next-c  ( -- #chars )  c 1 fid read-file s" next-c error1" gthrow  ;

defer in-quoted-field

: at-quoted-field-end?  ( -- ok? )
    r> drop  next-c 1 <>  if  false  else
        c c@ case
            [char] "  of  c 1 buffer+  in-quoted-field  endof
            true swap
        endcase
    then  ;

: (in-quoted-field)  ( -- ok? )
    r> drop  next-c 1 <>  if  false  else
        c c@ case
            [char] "  of  at-quoted-field-end?  endof
            drop c 1 buffer+ recurse
        endcase
    then  ;

' (in-quoted-field) is in-quoted-field

: in-bare-field  ( -- ok? )
    r> drop  next-c 1 <>  if  false  else
        c c@ case
            [char] ,  of  true  endof
            10        of  true  endof
            13        of  true  endof
            drop c 1 buffer+  recurse
        endcase
    then  ;

: before-field  ( -- ok? )
    r> drop  next-c 1 <>  if  false  else
        c c@ case
            [char] "  of  in-quoted-field  endof
            [char] ,  of  true  endof
            10        of  true  endof
            13        of  true  endof
            drop c 1 buffer+  in-bare-field
        endcase
    then  ;

}private  \ }}}

: with-csv-file-id  ( fid -- )  to fid  ;
: close-csv-file  ( -- )  fid close-file s" close-csv-file error1" gthrow  ;

: csv-field>buffer  ( -- ok? )  0 to bufferlen  before-field  ;

: type-buffer  ( -- )  buffer bufferlen type  ;

privatize

: (test)  ( in-loop? -- )
    if  r> drop  then  field>buffer  if  ." next:" type-buffer cr  true recurse  then  ;
: test  ( -- )  s" test.csv" r/o open-file throw with-csv-file-id  cr false (test)  close-csv-file  ;

