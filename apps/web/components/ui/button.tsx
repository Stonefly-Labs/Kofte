import * as React from "react";
import { Slot } from "@radix-ui/react-slot";
import { cva } from "class-variance-authority";
import { clsx } from "clsx";
import { twMerge } from "tailwind-merge";

const styles = cva(
  "inline-flex min-h-11 items-center justify-center gap-2 rounded-lg bg-[#203b32] px-5 py-2.5 text-sm font-semibold text-white hover:bg-[#345a49] disabled:pointer-events-none",
);

export function Button({
  className,
  asChild = false,
  ...props
}: React.ComponentProps<"button"> & { asChild?: boolean }) {
  const Component = asChild ? Slot : "button";
  return (
    <Component className={twMerge(clsx(styles(), className))} {...props} />
  );
}
