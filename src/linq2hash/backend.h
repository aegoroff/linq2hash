﻿/*!
 * \brief   The file contains backend interface
 * \author  \verbatim
            Created by: Alexander Egorov
            \endverbatim
 * \date    \verbatim
            Creation date: 2015-08-22
            \endverbatim
 * Copyright: (c) Alexander Egorov 2009-2016
 */

#ifndef LINQ2HASH_BACKEND_H_
#define LINQ2HASH_BACKEND_H_
#include "frontend.h"

typedef enum opcode_t {
    opcode_undefined = -1,
    opcode_from,
    opcode_def,
    opcode_let,
    opcode_select,
    opcode_call,
    opcode_property
} opcode_t;

typedef struct triple_t {
    opcode_t code;
    void* op1;
    void* op2;
} triple_t;

void bend_init(apr_pool_t* pool);
void bend_cleanup();

void bend_print_label(fend_node_t* node, apr_pool_t* pool);
void bend_emit(fend_node_t* node, apr_pool_t* pool);
char* bend_create_label(fend_node_t* t, apr_pool_t* pool);
triple_t* bend_create_triple(fend_node_t* t, apr_pool_t* pool);
BOOL bend_match_re(const char* pattern, const char* subject);

#endif // LINQ2HASH_BACKEND_H_