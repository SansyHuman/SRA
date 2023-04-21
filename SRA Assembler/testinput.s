.data
line_feed:  .byte 0x0A

.text
.global println_asciiz: // a0: null-terminated ascii string
    push    %a0
    addi    %ret, %zero, 4
    syscall // print string
    la      %t0, line_feed
    lbi     %a0, [%t0]0 // a0 = '\n'
    addi    %ret, %zero, 11
    syscall // print character
    pop     %a0
    ret