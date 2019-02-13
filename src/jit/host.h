// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*****************************************************************************/

#ifdef DEBUG

#undef printf
#define printf logf

#undef fprintf
#define fprintf flogf

class Compiler;
class LogEnv
{
public:
    LogEnv(ICorJitInfo* aCompHnd);
    void setCompiler(Compiler* val)
    {
        const_cast<Compiler*&>(compiler) = val;
    }

    ICorJitInfo* const compHnd;
    Compiler* const    compiler;
};

BOOL vlogf(unsigned level, const char* fmt, va_list args);
int vflogf(FILE* file, const char* fmt, va_list args);

int logf(const char* fmt, ...);
int flogf(FILE* file, const char* fmt, ...);
void gcDump_logf(const char* fmt, ...);

void logf(unsigned level, const char* fmt, ...);

extern "C" void __cdecl assertAbort(const char* why, const char* file, unsigned line);

#undef assert
//#define assert(p) (void)((p) || (assertAbort(#p, __FILE__, __LINE__), 0))
#define assert(p) (void)((p) || (DebugBreak(), 0))

#else // DEBUG

#undef assert
#define assert(p) (void)0
#endif // DEBUG

/*****************************************************************************/
#ifndef _HOST_H_
#define _HOST_H_
/*****************************************************************************/

extern FILE* jitstdout;

inline FILE* procstdout()
{
    return stdout;
}
#undef stdout
#define stdout use_jitstdout

/*****************************************************************************/
#endif
/*****************************************************************************/
