#include "http_parser_interop.h"

size_t http_parser_sizeof_size_t()
{
	return sizeof(size_t);
}
void http_parser_get_sizeof_types(struct http_parser_sizeof_types* sizeofTypes)
{
	sizeofTypes->sizeofShort = sizeof(short);
	sizeofTypes->sizeofInt = sizeof(int);
	sizeofTypes->sizeofLong = sizeof(long);
	sizeofTypes->sizeofHttpParser = sizeof(struct http_parser);
	sizeofTypes->sizeofHttpParserSettings = sizeof(struct http_parser_settings);
}

// Returns 0 on success
int http_parser_set_callback(http_parser_settings* settings, enum CallbackID id, void* callback)
{
	switch(id)
	{
	case HTTP_PARSER_ON_MESSAGE_BEGIN   : settings->on_message_begin    = callback; break;
	case HTTP_PARSER_ON_URL             : settings->on_url              = callback; break;
	case HTTP_PARSER_ON_STATUS          : settings->on_status           = callback; break;
	case HTTP_PARSER_ON_HEADER_FIELD    : settings->on_header_field     = callback; break;
	case HTTP_PARSER_ON_HEADER_VALUE    : settings->on_header_value     = callback; break;
	case HTTP_PARSER_ON_HEADERS_COMPLETE: settings->on_headers_complete = callback; break;
	case HTTP_PARSER_ON_BODY            : settings->on_body             = callback; break;
	case HTTP_PARSER_ON_MESSAGE_COMPLETE: settings->on_message_complete = callback; break;
	case HTTP_PARSER_ON_CHUNK_HEADER    : settings->on_chunk_header     = callback; break;
	case HTTP_PARSER_ON_CHUNK_COMPLETE  : settings->on_chunk_complete   = callback; break;
	default: return 1;
	}
	return 0;
}