﻿/*!
 * \brief   The file contains backend test class implementation
 * \author  \verbatim
            Created by: Alexander Egorov
            \endverbatim
 * \date    \verbatim
            Creation date: 2015-08-27
            \endverbatim
 * Copyright: (c) Alexander Egorov 2009-2016
 */

#include "BackendTest.h"

TEST_F(BackendTest, MatchSuccess) {
    ASSERT_TRUE(bend_match_re("[0-9]+", "123"));
}

TEST_F(BackendTest, MatchFailure) {
    ASSERT_FALSE(bend_match_re("[0-9]+", "num"));
}