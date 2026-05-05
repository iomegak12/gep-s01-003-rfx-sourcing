export class AppError extends Error {
  constructor(title, status, detail, slug) {
    super(detail);
    this.name = this.constructor.name;
    this.title = title;
    this.status = status;
    this.detail = detail;
    this.slug = slug;
  }
}
