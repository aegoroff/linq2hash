/*!
 * \brief   The file contains file hash implementation
 * \author  \verbatim
            Created by: Alexander Egorov
            \endverbatim
 * \date    \verbatim
            Creation date: 2011-11-23
            \endverbatim
 * Copyright: (c) Alexander Egorov 2009-2011
 */

#include "apr_mmap.h"
#include "filehash.h"
#include "lib.h"
#include "encoding.h"
#include "implementation.h"

#define FILE_BIG_BUFFER_SIZE 1 * BINARY_THOUSAND * BINARY_THOUSAND  // 1 megabyte

apr_status_t CalculateFile(const char* fullPathToFile, DataContext* ctx, apr_pool_t* pool)
{
    apr_byte_t digest[DIGESTSIZE];
    size_t len = 0;
    apr_status_t status = APR_SUCCESS;

    if (!CalculateFileHash(fullPathToFile, digest, ctx->IsPrintCalcTime,
                           ctx->HashToSearch, ctx->Limit, ctx->Offset, ctx->PfnOutput, pool)) {
        return status;
    }

    OutputDigest(digest, ctx, pool);

    if (!(ctx->FileToSave)) {
        return status;
    }
    apr_file_printf(ctx->FileToSave, HashToString(digest, ctx->IsPrintLowCase, pool));

    len = strlen(fullPathToFile);

    while (len > 0 && *(fullPathToFile + (len - 1)) != PATH_ELT_SEPARATOR) {
        --len;
    }

    apr_file_printf(ctx->FileToSave,
                    HASH_FILE_COLUMN_SEPARATOR "%s" NEW_LINE,
                    fullPathToFile + len);
    return status;
}

void OutputDigest(apr_byte_t* digest, DataContext* ctx, apr_pool_t* pool)
{
    OutputContext output = { 0 };
    output.IsFinishLine = TRUE;
    output.IsPrintSeparator = FALSE;
    output.StringToPrint = HashToString(digest, ctx->IsPrintLowCase, pool);
    ctx->PfnOutput(&output);
}

int CalculateFileHash(const char* filePath,
                      apr_byte_t* digest,
                      int         isPrintCalcTime,
                      const char* hashToSearch,
                      apr_off_t   limit,
                      apr_off_t   offset,
                      void        (* PfnOutput)(OutputContext* ctx),
                      apr_pool_t* pool)
{
    apr_file_t* fileHandle = NULL;
    apr_finfo_t info = { 0 };
    hash_context_t context = { 0 };
    apr_status_t status = APR_SUCCESS;
    int result = TRUE;
    apr_off_t pageSize = 0;
    apr_off_t filePartSize = 0;
    apr_off_t startOffset = offset;
    apr_mmap_t* mmap = NULL;
    char* fileAnsi = NULL;
    int isZeroSearchHash = FALSE;
    apr_byte_t digestToCompare[DIGESTSIZE];
    OutputContext output = { 0 };

    fileAnsi = FromUtf8ToAnsi(filePath, pool);
    if (!hashToSearch) {
        output.StringToPrint = fileAnsi == NULL ? filePath : fileAnsi;
        output.IsPrintSeparator = TRUE;
        PfnOutput(&output);
    }
    StartTimer();

    status = apr_file_open(&fileHandle, filePath, APR_READ | APR_BINARY, APR_FPROT_WREAD, pool);
    if (status != APR_SUCCESS) {
        OutputErrorMessage(status, PfnOutput, pool);
        return FALSE;
    }
    status = InitContext(&context);
    if (status != APR_SUCCESS) {
        OutputErrorMessage(status, PfnOutput, pool);
        result = FALSE;
        goto cleanup;
    }

    status = apr_file_info_get(&info, APR_FINFO_NAME | APR_FINFO_MIN, fileHandle);

    if (status != APR_SUCCESS) {
        OutputErrorMessage(status, PfnOutput, pool);
        result = FALSE;
        goto cleanup;
    }

    if (!hashToSearch) {
        output.IsPrintSeparator = TRUE;
        output.IsFinishLine = FALSE;
        output.StringToPrint = CopySizeToString(info.size, pool);
        PfnOutput(&output);
    }

    if (hashToSearch) {
        ToDigest(hashToSearch, digestToCompare);
        status = CalculateDigest(digest, NULL, 0);
        if (CompareDigests(digest, digestToCompare)) { // Empty file optimization
            isZeroSearchHash = TRUE;
            goto endtiming;
        }
    }

    filePartSize = MIN(limit, info.size);

    if (filePartSize > FILE_BIG_BUFFER_SIZE) {
        pageSize = FILE_BIG_BUFFER_SIZE;
    } else if (filePartSize == 0) {
        status = CalculateDigest(digest, NULL, 0);
        goto endtiming;
    } else {
        pageSize = filePartSize;
    }

    if (offset >= info.size) {
        output.IsFinishLine = TRUE;
        output.IsPrintSeparator = FALSE;
        output.StringToPrint = "Offset is greater then file size";
        PfnOutput(&output);
        result = FALSE;
        goto endtiming;
    }

    do {
        apr_status_t hashCalcStatus = APR_SUCCESS;
        apr_size_t size = (apr_size_t)MIN(pageSize, (filePartSize + startOffset) - offset);

        if (size + offset > info.size) {
            size = info.size - offset;
        }

        status =
            apr_mmap_create(&mmap, fileHandle, offset, size, APR_MMAP_READ, pool);
        if (status != APR_SUCCESS) {
            OutputErrorMessage(status, PfnOutput, pool);
            result = FALSE;
            mmap = NULL;
            goto cleanup;
        }
        hashCalcStatus = UpdateHash(&context, mmap->mm, mmap->size);
        if (hashCalcStatus != APR_SUCCESS) {
            OutputErrorMessage(hashCalcStatus, PfnOutput, pool);
            result = FALSE;
            goto cleanup;
        }
        offset += mmap->size;
        status = apr_mmap_delete(mmap);
        if (status != APR_SUCCESS) {
            OutputErrorMessage(status, PfnOutput, pool);
            mmap = NULL;
            result = FALSE;
            goto cleanup;
        }
        mmap = NULL;
    } while (offset < filePartSize + startOffset && offset < info.size);
    status = FinalHash(digest, &context);
endtiming:
    StopTimer();

    if (!hashToSearch) {
        goto printtime;
    }

    result = FALSE;
    if (!((!isZeroSearchHash &&
           CompareDigests(digest, digestToCompare)) || (isZeroSearchHash && (info.size == 0) ))) {
        goto printtime;
    }

    output.IsFinishLine = FALSE;
    output.IsPrintSeparator = TRUE;

    // file name
    output.StringToPrint = fileAnsi == NULL ? filePath : fileAnsi;
    PfnOutput(&output);

    // file size
    output.StringToPrint = CopySizeToString(info.size, pool);

    if (isPrintCalcTime) {
        output.IsPrintSeparator = TRUE;
        PfnOutput(&output); // file size output before time

        // time
        output.StringToPrint = CopyTimeToString(ReadElapsedTime(), pool);
    }
    output.IsFinishLine = TRUE;
    output.IsPrintSeparator = FALSE;
    PfnOutput(&output); // file size or time output

printtime:
    if (isPrintCalcTime & !hashToSearch) {
        // time
        output.StringToPrint = CopyTimeToString(ReadElapsedTime(), pool);
        output.IsFinishLine = FALSE;
        output.IsPrintSeparator = TRUE;
        PfnOutput(&output);
    }
    if (status != APR_SUCCESS) {
        OutputErrorMessage(status, PfnOutput, pool);
    }
cleanup:
    if (mmap != NULL) {
        status = apr_mmap_delete(mmap);
        mmap = NULL;
        if (status != APR_SUCCESS) {
            OutputErrorMessage(status, PfnOutput, pool);
        }
    }
    status = apr_file_close(fileHandle);
    if (status != APR_SUCCESS) {
        OutputErrorMessage(status, PfnOutput, pool);
    }
    return result;
}