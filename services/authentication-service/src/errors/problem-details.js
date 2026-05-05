const BASE_URI = 'https://rfx.gep.local/probs';

export function toProblemDetails(appErr, req) {
  return {
    type: `${BASE_URI}/${appErr.slug}`,
    title: appErr.title,
    status: appErr.status,
    detail: appErr.detail,
    instance: req.originalUrl || req.url,
    correlationId: req.correlationId,
  };
}
