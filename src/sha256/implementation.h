/*!
 * \brief   The file contains SHA256 calculator implementation defines
 * \author  \verbatim
            Created by: Alexander Egorov
            \endverbatim
 * \date    \verbatim
            Creation date: 2010-07-22
            \endverbatim
 * Copyright: (c) Alexander Egorov 2009-2013
 */

#ifndef SHA256_IMPLEMENTATION_H_
#define SHA256_IMPLEMENTATION_H_

#include "apr.h"
#include "apr_errno.h"
#include "sph_sha2.h"

typedef sph_sha256_context hash_context_t;

#define CALC_DIGEST_NOT_IMPLEMETED
#define SHA256_HASH_SIZE (SPH_SIZE_sha256/8)
#define DIGESTSIZE SHA256_HASH_SIZE
#define APP_NAME "SHA256 Calculator " PRODUCT_VERSION
#define HASH_NAME "SHA256"
#define OPT_HASH_LONG "sha256"

#endif // SHA256_IMPLEMENTATION_H_