.data
line_feed:  .byte 0x0A
.unicode "Hello world!"

.text
.entry println_asciiz
.global println_asciiz: // a0: null-terminated ascii string
    push    %a0
    addi    %ret, %zero, 4
    syscall // print string
    la      %t0, line_feed
    lbi     %a0, [%t0]0 // a0 = '\n'
    addi    %ret, %zero, 11
    syscall // print character
    pop     %a0
    ecall
    ret

.ktext 0x8000000000
    krr %t0, %cause
    addi %t1, %t1, 1
    addi %t2, %zero, 31
    sll %t1, %t1, %t2
    or %t0, %t0, %t1
    krw %cause, %t0
    la %a0, k_hello_world
    addi %ret, %zero, 4
    syscall
    eret

.kdata
k_hello_world: .asciiz "Hello world! Kernel"