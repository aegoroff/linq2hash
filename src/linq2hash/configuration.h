﻿/*!
 * \brief   The file contains configuration module interface
 * \author  \verbatim
            Created by: Alexander Egorov
            \endverbatim
 * \date    \verbatim
            Creation date: 2015-09-01
            \endverbatim
 * Copyright: (c) Alexander Egorov 2015
 */


#ifndef LINQ2HASH_CONFIGURATION_H_
#define LINQ2HASH_CONFIGURATION_H_

typedef struct configuration_ctx_t {
    void (*on_string)(const char* const str);
    void (*on_file)(struct arg_file* files);
    int argc;
    char** argv;
} configuration_ctx_t;

void conf_configure_app(configuration_ctx_t* ctx);

#endif // LINQ2HASH_CONFIGURATION_H_
